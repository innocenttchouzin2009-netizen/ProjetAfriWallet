namespace MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

public sealed record CameroonOperatorResolution(
    string NormalizedPhoneNumber,
    CameroonMobileOperator? Operator)
{
    public bool IsResolved => Operator is not null;
}
