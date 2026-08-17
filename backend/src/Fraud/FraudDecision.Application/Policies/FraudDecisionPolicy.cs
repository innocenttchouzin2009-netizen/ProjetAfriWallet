using AfriWallet.Fraud.Decision.Domain.Decisions;

namespace AfriWallet.Fraud.Decision.Application.Policies;

public sealed class FraudDecisionPolicy
{
    public FraudDecisionBand ResolveBand(int score) => score switch
    {
        >= 80 => FraudDecisionBand.Critical,
        >= 60 => FraudDecisionBand.High,
        >= 30 => FraudDecisionBand.Medium,
        _ => FraudDecisionBand.Low
    };

    public FraudDecisionAction ResolveAction(int score, bool criticalOverride)
    {
        if (criticalOverride)
            return FraudDecisionAction.DeclineRecommended;

        return score switch
        {
            >= 80 => FraudDecisionAction.DeclineRecommended,
            >= 60 => FraudDecisionAction.Challenge,
            >= 30 => FraudDecisionAction.Review,
            _ => FraudDecisionAction.Allow
        };
    }
}