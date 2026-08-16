using AfriWallet.Fraud.TransactionFraud.Domain.Detection;

namespace AfriWallet.Fraud.TransactionFraud.Application.Policies;

public sealed class TransactionFraudPolicy
{
    public FraudDetectionBand ResolveBand(int score) => score switch
    {
        >= 80 => FraudDetectionBand.Critical,
        >= 60 => FraudDetectionBand.High,
        >= 30 => FraudDetectionBand.Medium,
        _ => FraudDetectionBand.Low
    };

    public FraudDetectionRecommendation ResolveRecommendation(int score) => score switch
    {
        >= 80 => FraudDetectionRecommendation.DeclineRecommended,
        >= 60 => FraudDetectionRecommendation.Challenge,
        >= 30 => FraudDetectionRecommendation.Review,
        _ => FraudDetectionRecommendation.Allow
    };
}
