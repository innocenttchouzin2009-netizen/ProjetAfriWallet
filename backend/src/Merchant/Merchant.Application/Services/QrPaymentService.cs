using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed record GenerateQrCommand(
    string MerchantId,
    QrPaymentType Type,
    decimal Amount,
    string Currency,
    string MerchantName,
    string Description);

public sealed record InitiateQrPaymentCommand(
    string QrId,
    string PayerWalletId,
    decimal Amount,
    string Currency);

public sealed record DecodedQrPayload(
    QrPaymentType Type,
    string MerchantId,
    decimal Amount,
    string Currency,
    string MerchantName,
    string Description);

public sealed record QrReceiptPayload(
    string ReceiptId,
    string ReceiptCode);

public sealed class QrPaymentService
{
    private readonly List<QrPayment> _payments = [];
    private readonly Dictionary<string, List<string>> _timeline = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<QrPayment>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<QrPayment>>(_payments);

    public Task<QrPayment> CreateAsync(QrPayment payment, CancellationToken cancellationToken = default)
    {
        payment.PaymentId = string.IsNullOrWhiteSpace(payment.PaymentId) ? Guid.NewGuid().ToString("N") : payment.PaymentId;
        payment.QrId = string.IsNullOrWhiteSpace(payment.QrId) ? $"qr-{Guid.NewGuid():N}" : payment.QrId;
        payment.CreatedAt = payment.CreatedAt == default ? DateTimeOffset.UtcNow : payment.CreatedAt;
        payment.UpdatedAt = payment.UpdatedAt == default ? DateTimeOffset.UtcNow : payment.UpdatedAt;
        payment.Status = string.IsNullOrWhiteSpace(payment.Status) ? QrPaymentStatus.Active.ToString() : payment.Status;
        _payments.Add(payment);
        return Task.FromResult(payment);
    }

    public QrPayment GenerateQr(GenerateQrCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MerchantId))
        {
            throw new InvalidOperationException("Merchant identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            throw new InvalidOperationException("Currency is required.");
        }

        if (command.Type == QrPaymentType.Static && command.Amount <= 0)
        {
            throw new InvalidOperationException("Static QR payments require a positive amount.");
        }

        var payment = new QrPayment
        {
            PaymentId = Guid.NewGuid().ToString("N"),
            QrId = $"qr-{Guid.NewGuid():N}",
            MerchantId = command.MerchantId,
            Type = command.Type,
            AmountMinor = command.Amount,
            Currency = command.Currency.ToUpperInvariant(),
            MerchantName = command.MerchantName,
            Description = command.Description,
            Code = BuildQrCode(command),
            Status = QrPaymentStatus.Active.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = command.Type == QrPaymentType.Dynamic ? DateTimeOffset.UtcNow.AddHours(24) : null
        };

        _payments.Add(payment);
        return payment;
    }

    public DecodedQrPayload DecodeQr(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("QR code is required.");
        }

        var parts = code.Split('|');
        if (parts.Length < 5)
        {
            throw new InvalidOperationException("The QR code is invalid.");
        }

        var type = Enum.TryParse<QrPaymentType>(parts[1], ignoreCase: true, out var parsedType) ? parsedType : QrPaymentType.Static;
        return new DecodedQrPayload(
            Type: type,
            MerchantId: parts[2],
            Amount: decimal.Parse(parts[3]),
            Currency: parts[4],
            MerchantName: parts.Length > 5 ? parts[5] : string.Empty,
            Description: parts.Length > 6 ? parts[6] : string.Empty);
    }

    public QrPayment InitiatePayment(InitiateQrPaymentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.QrId))
        {
            throw new InvalidOperationException("QR identifier is required.");
        }

        var payment = _payments.SingleOrDefault(item => string.Equals(item.QrId, command.QrId, StringComparison.OrdinalIgnoreCase));
        if (payment is null)
        {
            throw new InvalidOperationException("QR payment was not found.");
        }

        var transferIntentId = Guid.NewGuid().ToString("N");
        payment.TransferIntentId = transferIntentId;
        payment.Status = QrPaymentStatus.Initiated.ToString();
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        _timeline[transferIntentId] =
        [
            $"{DateTimeOffset.UtcNow:O} QR received for {payment.MerchantId}",
            $"{DateTimeOffset.UtcNow:O} Payment initiated for wallet {command.PayerWalletId}"
        ];

        return payment;
    }

    public QrReceiptPayload GenerateReceipt(string transferIntentId)
    {
        if (string.IsNullOrWhiteSpace(transferIntentId))
        {
            throw new InvalidOperationException("Transfer identifier is required.");
        }

        var receiptId = $"receipt-{Guid.NewGuid():N}";
        var receiptCode = $"AFW-{transferIntentId[..8].ToUpperInvariant()}";
        var payment = _payments.SingleOrDefault(item => string.Equals(item.TransferIntentId, transferIntentId, StringComparison.OrdinalIgnoreCase));
        if (payment is not null)
        {
            payment.ReceiptId = receiptId;
            payment.ReceiptCode = receiptCode;
            payment.Status = QrPaymentStatus.Paid.ToString();
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return new QrReceiptPayload(receiptId, receiptCode);
    }

    public IReadOnlyList<string> GetTimeline(string transferIntentId)
    {
        return _timeline.TryGetValue(transferIntentId, out var entries)
            ? entries.AsReadOnly()
            : Array.Empty<string>();
    }

    private static string BuildQrCode(GenerateQrCommand command)
    {
        var amount = command.Type == QrPaymentType.Dynamic ? 0m : command.Amount;
        return $"AFW|{command.Type}|{command.MerchantId}|{amount}|{command.Currency.ToUpperInvariant()}|{command.MerchantName}|{command.Description}";
    }
}
