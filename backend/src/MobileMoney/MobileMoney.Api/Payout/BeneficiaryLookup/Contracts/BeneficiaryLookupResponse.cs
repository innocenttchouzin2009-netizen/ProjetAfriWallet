using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

public sealed record BeneficiaryLookupResponse(
    string NormalizedPhoneNumber,
    string CountryCode,
    CameroonMobileOperator? Operator,
    bool OperatorResolved);
