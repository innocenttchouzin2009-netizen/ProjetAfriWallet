namespace MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutFundingPlanEntity
{
    public Guid CorrelationId { get; set; }
    public long RequiredAmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int AllocationCount { get; set; }
    public DateTimeOffset PlannedAtUtc { get; set; }
}
