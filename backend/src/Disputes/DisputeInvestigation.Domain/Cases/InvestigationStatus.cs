namespace AfriWallet.Disputes.Investigation.Domain.Cases;

public enum InvestigationStatus
{
    Open = 0,
    Assigned = 1,
    WaitingForEvidence = 2,
    UnderReview = 3,
    Completed = 4,
    Closed = 5
}
