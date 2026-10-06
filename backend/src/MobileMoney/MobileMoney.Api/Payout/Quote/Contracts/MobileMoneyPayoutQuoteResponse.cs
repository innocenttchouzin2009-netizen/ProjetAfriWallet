namespace MobileMoney.Production.Payout.Quote.Contracts;

public sealed record MobileMoneyPayoutQuoteResponse(
    Guid QuoteId,
    string SourceCurrency,
    long SourceAmountMinor,
    IReadOnlyList<MobileMoneyPayoutQuoteFeeResponse> Fees,
    long TotalFeeMinor,
    long TotalSourceDebitMinor,
    string DestinationCurrency,
    long DestinationAmountMinor,
    decimal FxRate,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
