namespace MobileMoney.Production.Payout.Contracts;

public sealed record MobileMoneyBeneficiaryResponse(
    string Msisdn,
    string CountryCode,
    string OperatorCode,
    string? DisplayName);
