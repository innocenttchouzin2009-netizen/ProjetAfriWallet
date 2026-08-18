namespace AfriWallet.Disputes.Decision.Domain.Decisions;

/// Business recommendation only; never an execution instruction.
public enum ResolutionDecisionType
{
    ManualReview = 0,
    RefundRecommended = 1,
    ChargebackRecommended = 2,
    Decline = 3
}
