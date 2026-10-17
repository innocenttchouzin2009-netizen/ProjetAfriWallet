namespace MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutExecutionBindingEntity
{
    public Guid BindingId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    public Guid QuoteId { get; set; }
    public string QuoteSourceCurrency { get; set; } = string.Empty;
    public long QuoteSourceAmountMinor { get; set; }
    public long QuoteTotalFeeMinor { get; set; }
    public long QuoteTotalSourceDebitMinor { get; set; }
    public string QuoteDestinationCurrency { get; set; } = string.Empty;
    public long QuoteDestinationAmountMinor { get; set; }
    public DateTimeOffset QuoteExpiresAtUtc { get; set; }

    public Guid FundingCorrelationId { get; set; }
    public long FundingRequiredAmountMinor { get; set; }
    public string FundingCurrencyCode { get; set; } = string.Empty;
    public string FundingAllocationsJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
