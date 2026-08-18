namespace AfriWallet.Disputes.Resolution.Domain.Resolutions;

public enum ResolutionReasonCode
{
    DecisionApproved = 0,
    RefundRouteSelected = 1,
    ChargebackRouteSelected = 2,
    ProviderAccepted = 10,
    ProviderAcknowledged = 11,
    ProviderCompleted = 12,
    ProviderTimeout = 20,
    ProviderTemporaryFailure = 21,
    ProviderPermanentFailure = 22,
    RetryScheduled = 23,
    RetryExhausted = 24,
    CompensationRequired = 30,
    CompensationCompleted = 31,
    InvalidDecision = 40,
    DecisionNotApproved = 41,
    UnsupportedDecisionType = 42,
    ManualInterventionRequired = 50
}
