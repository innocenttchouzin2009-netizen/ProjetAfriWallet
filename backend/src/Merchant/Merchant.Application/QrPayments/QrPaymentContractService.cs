using AfriWallet.Merchant.Application.Contracts.QrPayments;
using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.QrPayments;

public sealed class QrPaymentContractService(QrPaymentService service)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, InitiationRecord> _byIdempotency = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InitiationRecord> _byTransfer = new(StringComparer.OrdinalIgnoreCase);

    public async Task<DecodedQrResponse> DecodeAsync(
        DecodeQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Require(request.Code, "QR code is required.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var payments = await service.GetAllAsync(cancellationToken);
            var payment = payments.SingleOrDefault(item =>
                string.Equals(item.Code, code, StringComparison.Ordinal));

            if (payment is null)
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.NotFound,
                    "QR payment was not found.");
            }

            return new DecodedQrResponse(
                payment.QrId,
                payment.Type.ToString(),
                payment.MerchantId,
                ToMinorUnits(payment.AmountMinor),
                payment.Currency,
                payment.MerchantName,
                payment.Description,
                EffectiveStatus(payment),
                payment.ExpiresAt);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<QrPaymentStatusResponse> InitiateAsync(
        InitiateQrPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AuthenticatedUserId == Guid.Empty)
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.Validation,
                "Authenticated user identifier is required.");
        }

        var qrId = Require(request.QrId, "QR identifier is required.");
        var payerWalletId = Require(request.PayerWalletId, "Payer wallet identifier is required.");
        var currency = NormalizeCurrency(request.Currency);
        var idempotencyKey = Require(request.IdempotencyKey, "Idempotency key is required.");

        if (idempotencyKey.Length > 128)
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.Validation,
                "Idempotency key must not exceed 128 characters.");
        }

        if (request.AmountMinor <= 0)
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.Validation,
                "Payment amount must be positive.");
        }

        var scopedKey = $"{request.AuthenticatedUserId:N}:{idempotencyKey}";

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_byIdempotency.TryGetValue(scopedKey, out var previous))
            {
                if (!previous.Matches(qrId, payerWalletId, request.AmountMinor, currency))
                {
                    throw Error(
                        QrPaymentContractValues.ErrorCodes.IdempotencyConflict,
                        "Idempotency key has already been used for a different QR payment request.");
                }

                return await ReadStatusCoreAsync(previous.TransferIntentId, cancellationToken);
            }

            var payments = await service.GetAllAsync(cancellationToken);
            var payment = payments.SingleOrDefault(item =>
                string.Equals(item.QrId, qrId, StringComparison.OrdinalIgnoreCase));

            if (payment is null)
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.NotFound,
                    "QR payment was not found.");
            }

            if (payment.ExpiresAt is not null &&
                payment.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.Expired,
                    "QR payment has expired.");
            }

            if (!string.Equals(
                    payment.Status,
                    QrPaymentStatus.Active.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.NotActive,
                    "QR payment is not active.");
            }

            if (!string.Equals(payment.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.Validation,
                    "Payment currency does not match the QR currency.");
            }

            if (payment.Type == QrPaymentType.Static &&
                ToMinorUnits(payment.AmountMinor) != request.AmountMinor)
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.Validation,
                    "Payment amount does not match the static QR amount.");
            }

            var initiated = service.InitiatePayment(
                new InitiateQrPaymentCommand(
                    payment.QrId,
                    payerWalletId,
                    request.AmountMinor,
                    currency));

            if (initiated.Type == QrPaymentType.Dynamic)
            {
                initiated.AmountMinor = request.AmountMinor;
                initiated.Currency = currency;
                initiated.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (string.IsNullOrWhiteSpace(initiated.TransferIntentId))
            {
                throw new InvalidOperationException(
                    "QR payment initiation did not produce a transfer identifier.");
            }

            var record = new InitiationRecord(
                request.AuthenticatedUserId,
                idempotencyKey,
                qrId,
                payerWalletId,
                request.AmountMinor,
                currency,
                initiated.TransferIntentId);

            _byIdempotency.Add(scopedKey, record);
            _byTransfer.Add(initiated.TransferIntentId, record);

            return ToStatus(initiated);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<QrPaymentStatusResponse> GetStatusAsync(
        Guid authenticatedUserId,
        string transferIntentId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty)
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.NotFound,
                "QR payment transfer was not found.");
        }

        var normalizedTransferId = Require(
            transferIntentId,
            "Transfer identifier is required.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_byTransfer.TryGetValue(normalizedTransferId, out var record) ||
                record.AuthenticatedUserId != authenticatedUserId)
            {
                throw Error(
                    QrPaymentContractValues.ErrorCodes.NotFound,
                    "QR payment transfer was not found.");
            }

            return await ReadStatusCoreAsync(normalizedTransferId, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<QrPaymentStatusResponse> ReadStatusCoreAsync(
        string transferIntentId,
        CancellationToken cancellationToken)
    {
        var payments = await service.GetAllAsync(cancellationToken);
        var payment = payments.SingleOrDefault(item =>
            string.Equals(
                item.TransferIntentId,
                transferIntentId,
                StringComparison.OrdinalIgnoreCase));

        if (payment is null)
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.NotFound,
                "QR payment transfer was not found.");
        }

        return ToStatus(payment);
    }

    private static QrPaymentStatusResponse ToStatus(QrPayment payment)
    {
        if (string.IsNullOrWhiteSpace(payment.TransferIntentId))
        {
            throw new InvalidOperationException(
                "QR payment does not have a transfer identifier.");
        }

        return new QrPaymentStatusResponse(
            payment.PaymentId,
            payment.QrId,
            payment.TransferIntentId,
            EffectiveStatus(payment),
            ToMinorUnits(payment.AmountMinor),
            payment.Currency,
            payment.ReceiptId,
            payment.ReceiptCode,
            payment.UpdatedAt);
    }

    private static string EffectiveStatus(QrPayment payment) =>
        payment.ExpiresAt is not null &&
        payment.ExpiresAt <= DateTimeOffset.UtcNow &&
        string.Equals(
            payment.Status,
            QrPaymentStatus.Active.ToString(),
            StringComparison.OrdinalIgnoreCase)
            ? QrPaymentStatus.Expired.ToString()
            : payment.Status;

    private static long ToMinorUnits(decimal value)
    {
        if (decimal.Truncate(value) != value ||
            value > long.MaxValue ||
            value < long.MinValue)
        {
            throw new InvalidOperationException(
                "QR payment amount cannot be represented as minor units.");
        }

        return decimal.ToInt64(value);
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized = Require(value, "Currency is required.").ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.Validation,
                "Currency must use a three-letter code.");
        }

        return normalized;
    }

    private static string Require(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Error(
                QrPaymentContractValues.ErrorCodes.Validation,
                message);
        }

        return value.Trim();
    }

    private static QrPaymentContractException Error(
        string code,
        string message) => new(code, message);

    private sealed record InitiationRecord(
        Guid AuthenticatedUserId,
        string IdempotencyKey,
        string QrId,
        string PayerWalletId,
        long AmountMinor,
        string Currency,
        string TransferIntentId)
    {
        public bool Matches(
            string qrId,
            string payerWalletId,
            long amountMinor,
            string currency) =>
            string.Equals(QrId, qrId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(PayerWalletId, payerWalletId, StringComparison.OrdinalIgnoreCase) &&
            AmountMinor == amountMinor &&
            string.Equals(Currency, currency, StringComparison.OrdinalIgnoreCase);
    }
}
