namespace MobileMoney.Production.Payout.Quote.Contracts;

public sealed record CreateMobileMoneyPayoutQuoteRequest(
    string SourceCountryCode,
    string SourceCurrency,
    string DestinationCountryCode,
    string DestinationCurrency,
    string OperatorCode,
    long SourceAmountMinor);
