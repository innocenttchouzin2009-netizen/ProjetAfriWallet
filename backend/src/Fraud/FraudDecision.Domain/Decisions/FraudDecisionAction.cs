namespace AfriWallet.Fraud.Decision.Domain.Decisions;

public enum FraudDecisionAction
{
    Allow = 0,
    Review = 1,
    Challenge = 2,
    DeclineRecommended = 3
}