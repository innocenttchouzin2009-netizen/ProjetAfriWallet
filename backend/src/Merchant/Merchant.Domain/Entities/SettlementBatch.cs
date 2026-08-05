namespace AfriWallet.Merchant.Domain.Entities;

public sealed class SettlementBatch
{
    public string BatchId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public List<string> SettlementIds { get; set; } = [];
    public SettlementStatus Status { get; set; } = SettlementStatus.CREATED;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
