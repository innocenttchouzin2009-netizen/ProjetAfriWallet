namespace AfriWallet.Merchant.Domain.Entities;

public enum QrPaymentType
{
    Static,
    Dynamic
}

public enum QrPaymentStatus
{
    Active,
    Initiated,
    Paid,
    Expired
}

public sealed class QrPayment
{
    public string PaymentId { get; set; } = string.Empty;
    public string QrId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public QrPaymentType Type { get; set; } = QrPaymentType.Static;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = QrPaymentStatus.Active.ToString();
    public decimal AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TransferIntentId { get; set; }
    public string? ReceiptId { get; set; }
    public string? ReceiptCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
}
