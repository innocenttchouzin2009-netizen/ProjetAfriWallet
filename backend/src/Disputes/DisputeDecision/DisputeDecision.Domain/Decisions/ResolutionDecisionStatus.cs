namespace AfriWallet.Disputes.Decision.Domain.Decisions;

public enum ResolutionDecisionStatus
{
    Proposed = 0,
    PendingManualApproval = 1,
    Approved = 2,
    Declined = 3,
    Superseded = 4
}
