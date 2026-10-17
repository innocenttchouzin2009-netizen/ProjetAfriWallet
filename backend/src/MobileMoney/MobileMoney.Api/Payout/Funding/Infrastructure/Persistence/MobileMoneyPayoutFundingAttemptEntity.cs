namespace MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutFundingAttemptEntity
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public int Sequence { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public int SourceType { get; set; }
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? StatusReason { get; set; }
}
