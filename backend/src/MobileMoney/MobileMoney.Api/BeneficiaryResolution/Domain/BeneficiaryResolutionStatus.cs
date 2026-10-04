namespace MobileMoney.Production.BeneficiaryResolution.Domain;

public enum BeneficiaryResolutionStatus
{
    Resolved,
    OperatorConfirmationRequired,
    ManualEntryRequired,
    NotFound,
    Unsupported
}
