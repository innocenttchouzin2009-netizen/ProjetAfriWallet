namespace AfriWallet.Compliance.CaseManagement.Domain.Cases;

public enum ComplianceCaseDecision
{
    None = 0,
    FalsePositive = 1,
    ConfirmedRisk = 2,
    RequireAdditionalInformation = 3,
    RestrictAccount = 4
}