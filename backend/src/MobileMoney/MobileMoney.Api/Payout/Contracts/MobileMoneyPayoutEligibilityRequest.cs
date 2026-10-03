namespace MobileMoney.Production.Payout.Contracts;

public sealed record MobileMoneyPayoutEligibilityRequest(
    string SourceCountryCode,
    string SourceCurrency,
    string DestinationCountryCode,
    string DestinationCurrency,
    string OperatorCode);
