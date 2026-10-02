using AfriWallet.Merchant.Application.Contracts.QrPayments;
using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.QrPayments;

public sealed class QrPaymentApplicationService(IQrPaymentGateway gateway)
{
    public async Task<QrPaymentResponse> GenerateAsync(
        GenerateQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var type = ParseType(request.Type);
        ValidateMerchantId(request.MerchantId);
        var currency = NormalizeCurrency(request.Currency);

        if (type == QrPaymentType.Static && request.Amount <= 0m)
        {
            throw new InvalidOperationException(
                "Static QR payments require a positive amount.");
        }

        if (type == QrPaymentType.Dynamic && request.Amount != 0m)
        {
            throw new InvalidOperationException(
                "Dynamic QR generation must not lock an amount.");
        }

        var payment = await gateway.GenerateAsync(
            new GenerateQrCommand(
                request.MerchantId.Trim(),
                type,
                request.Amount,
                currency,
                request.MerchantName?.Trim() ?? string.Empty,
                request.Description?.Trim() ?? string.Empty),
            cancellationToken);

        return ToResponse(payment);
    }

    public async Task<DecodedQrResponse> DecodeAsync(
        DecodeQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateQrEnvelope(request.Code);

        var payload = await gateway.DecodeAsync(request.Code, cancellationToken);

        return new DecodedQrResponse(
            payload.Type.ToString(),
            payload.MerchantId,
            payload.Amount,
            payload.Currency,
            payload.MerchantName,
            payload.Description);
    }

    public async Task<QrPaymentResponse> InitiateAsync(
        InitiateQrPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QrId))
        {
            throw new InvalidOperationException("QR identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PayerWalletId))
        {
            throw new InvalidOperationException("Payer wallet identifier is required.");
        }

        if (request.Amount <= 0m)
        {
            throw new InvalidOperationException("Payment amount must be positive.");
        }

        var currency = NormalizeCurrency(request.Currency);
        var payment = await gateway.FindByQrIdAsync(
            request.QrId.Trim(),
            cancellationToken);

        if (payment is null)
        {
            throw new InvalidOperationException("QR payment was not found.");
        }

        if (!string.Equals(
                payment.Status,
                QrPaymentStatus.Active.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "QR payment is not active.");
        }

        if (payment.ExpiresAt is not null &&
            payment.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            payment.Status = QrPaymentStatus.Expired.ToString();
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            throw new InvalidOperationException("QR payment has expired.");
        }

        if (!string.Equals(
                payment.Currency,
                currency,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Payment currency does not match the QR currency.");
        }

        if (payment.Type == QrPaymentType.Static &&
            payment.AmountMinor != request.Amount)
        {
            throw new InvalidOperationException(
                "Payment amount does not match the static QR amount.");
        }

        var initiated = await gateway.InitiateAsync(
            new InitiateQrPaymentCommand(
                payment.QrId,
                request.PayerWalletId.Trim(),
                request.Amount,
                currency),
            cancellationToken);

        return ToResponse(initiated);
    }

    public async Task<QrReceiptResponse> GenerateReceiptAsync(
        QrReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TransferIntentId))
        {
            throw new InvalidOperationException(
                "Transfer identifier is required.");
        }

        var transferIntentId = request.TransferIntentId.Trim();
        var payment = await gateway.FindByTransferIntentIdAsync(
            transferIntentId,
            cancellationToken);

        if (payment is null)
        {
            throw new InvalidOperationException(
                "QR payment transfer was not found.");
        }

        if (!string.IsNullOrWhiteSpace(payment.ReceiptId) &&
            !string.IsNullOrWhiteSpace(payment.ReceiptCode))
        {
            return new QrReceiptResponse(
                payment.ReceiptId,
                payment.ReceiptCode);
        }

        var receipt = await gateway.GenerateReceiptAsync(
            transferIntentId,
            cancellationToken);

        return new QrReceiptResponse(
            receipt.ReceiptId,
            receipt.ReceiptCode);
    }

    public async Task<QrTimelineResponse> GetTimelineAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transferIntentId))
        {
            throw new InvalidOperationException(
                "Transfer identifier is required.");
        }

        var normalizedTransferIntentId = transferIntentId.Trim();
        var payment = await gateway.FindByTransferIntentIdAsync(
            normalizedTransferIntentId,
            cancellationToken);

        if (payment is null)
        {
            throw new InvalidOperationException(
                "QR payment transfer was not found.");
        }

        var items = await gateway.GetTimelineAsync(
            normalizedTransferIntentId,
            cancellationToken);

        return new QrTimelineResponse(
            normalizedTransferIntentId,
            items);
    }

    private static QrPaymentResponse ToResponse(QrPayment payment)
        => new(
            payment.PaymentId,
            payment.QrId,
            payment.MerchantId,
            payment.Type.ToString(),
            payment.Code,
            payment.Status,
            payment.AmountMinor,
            payment.Currency,
            payment.MerchantName,
            payment.Description,
            payment.TransferIntentId,
            payment.ReceiptId,
            payment.ReceiptCode,
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.ExpiresAt);

    private static QrPaymentType ParseType(string type)
    {
        if (!Enum.TryParse<QrPaymentType>(
                type?.Trim(),
                ignoreCase: true,
                out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new InvalidOperationException(
                "QR payment type is invalid.");
        }

        return parsed;
    }

    private static void ValidateMerchantId(string merchantId)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            throw new InvalidOperationException(
                "Merchant identifier is required.");
        }
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidOperationException("Currency is required.");
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new InvalidOperationException(
                "Currency must use a three-letter code.");
        }

        return normalized;
    }

    private static void ValidateQrEnvelope(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("QR code is required.");
        }

        var parts = code.Split('|');
        if (parts.Length < 5 ||
            !string.Equals(parts[0], "AFW", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The QR code is invalid.");
        }

        _ = ParseType(parts[1]);

        if (string.IsNullOrWhiteSpace(parts[2]) ||
            !decimal.TryParse(
                parts[3],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out _) ||
            string.IsNullOrWhiteSpace(parts[4]))
        {
            throw new InvalidOperationException("The QR code is invalid.");
        }
    }
}
