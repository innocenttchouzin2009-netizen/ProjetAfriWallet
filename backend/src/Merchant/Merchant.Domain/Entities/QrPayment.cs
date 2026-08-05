namespace AfriWallet.Merchant.Domain.Entities;

public sealed class QrPayment
{
    public string PaymentId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;
    public string Scheme { get; set; } = "EMVCO";
    public string Status { get; set; } = "ACTIVE";
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal AmountMinor { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
