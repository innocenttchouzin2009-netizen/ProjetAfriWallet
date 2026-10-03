namespace MobileMoney.Production.Payout.Contracts;

public sealed record MobileMoneyBeneficiaryRequest(
    string Msisdn,
    string CountryCode,
    string OperatorCode,
    string? DisplayName);
