namespace MobileMoney.Production.Payout.Quote.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutQuoteEntity
{
    public Guid QuoteId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string SourceCountryCode { get; set; } = string.Empty;
    public string SourceCurrency { get; set; } = string.Empty;
    public string DestinationCountryCode { get; set; } = string.Empty;
    public string DestinationCurrency { get; set; } = string.Empty;
    public string OperatorCode { get; set; } = string.Empty;
    public long SourceAmountMinor { get; set; }
    public long DestinationAmountMinor { get; set; }
    public decimal FxRate { get; set; }
    public string FeesJson { get; set; } = "[]";
    public long TotalFeeMinor { get; set; }
    public long TotalSourceDebitMinor { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public Guid? FundingCorrelationId { get; set; }
}
