using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

public sealed record BeneficiaryAccountHolderLookupResponse(
    string NormalizedPhoneNumber,
    string CountryCode,
    CameroonMobileOperator Operator,
    string? AccountHolderName,
    bool BeneficiaryResolved);
