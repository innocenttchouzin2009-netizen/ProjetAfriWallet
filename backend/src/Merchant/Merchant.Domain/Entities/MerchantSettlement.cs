namespace AfriWallet.Merchant.Domain.Entities;

public sealed class MerchantSettlement
{
    public string SettlementId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public decimal GrossAmountMinor { get; set; }
    public decimal FeeAmountMinor { get; set; }
    public decimal NetAmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
