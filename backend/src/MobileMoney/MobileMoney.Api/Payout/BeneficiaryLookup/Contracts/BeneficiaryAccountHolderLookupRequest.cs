using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

public sealed record BeneficiaryAccountHolderLookupRequest(
    string NormalizedPhoneNumber,
    CameroonMobileOperator Operator);
