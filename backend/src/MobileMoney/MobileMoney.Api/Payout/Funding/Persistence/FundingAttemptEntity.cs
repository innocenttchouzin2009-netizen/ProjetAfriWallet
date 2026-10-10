namespace MobileMoney.Production.Payout.Funding.Persistence;

public sealed class FundingAttemptEntity
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public int SourceType { get; set; }
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public string? StatusReason { get; set; }
}
