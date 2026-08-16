namespace AfriWallet.Compliance.CaseManagement.Domain.Cases;

public enum ComplianceCaseStatus
{
    Open = 0,
    Assigned = 1,
    UnderReview = 2,
    Escalated = 3,
    Resolved = 4,
    Closed = 5
}