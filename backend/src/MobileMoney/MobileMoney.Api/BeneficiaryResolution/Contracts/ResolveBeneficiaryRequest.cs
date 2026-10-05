namespace MobileMoney.Production.BeneficiaryResolution.Contracts;

public sealed record ResolveBeneficiaryRequest(
    string PhoneNumber,
    string? CountryCode = null,
    string? OperatorCode = null);
