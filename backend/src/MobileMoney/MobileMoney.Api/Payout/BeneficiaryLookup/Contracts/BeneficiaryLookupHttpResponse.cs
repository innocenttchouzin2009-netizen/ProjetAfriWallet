namespace MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

public sealed record BeneficiaryLookupHttpResponse(
    string NormalizedPhoneNumber,
    string CountryCode,
    string? Operator,
    bool OperatorResolved,
    string? AccountHolderName,
    bool BeneficiaryResolved);
