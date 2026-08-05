namespace AfriWallet.Merchant.Domain.Entities;

public sealed class SettlementPolicy
{
    public bool Immediate { get; set; } = true;
    public int DelayDays { get; set; }
    public decimal MinimumThresholdMinor { get; set; }
    public bool GroupByBatch { get; set; } = true;
}
