namespace AfriWallet.Merchant.Domain.Entities;

public sealed class SettlementTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string SettlementId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public decimal NetAmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public SettlementMethod SettlementMethod { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.CREATED;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
