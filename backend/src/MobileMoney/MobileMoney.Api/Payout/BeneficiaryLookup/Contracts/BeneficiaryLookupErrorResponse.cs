namespace MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

public sealed record BeneficiaryLookupErrorResponse(
    string Code,
    string Message);
