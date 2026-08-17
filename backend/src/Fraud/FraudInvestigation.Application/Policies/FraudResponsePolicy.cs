using AfriWallet.Fraud.Investigation.Domain.Responses;

namespace AfriWallet.Fraud.Investigation.Application.Policies;

public sealed class FraudResponsePolicy
{
    public FraudResponseType Recommend(int fraudScore, string action) => fraudScore switch
    {
        >= 80 => FraudResponseType.DeclineRecommended,
        >= 60 => FraudResponseType.ChallengeCustomer,
        >= 30 => FraudResponseType.Monitor,
        _ => FraudResponseType.NoAction
    };
}