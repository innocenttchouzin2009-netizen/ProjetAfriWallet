namespace AfriWallet.Fraud.TransactionFraud.Domain.Detection;

public enum FraudDetectionRecommendation
{
    Allow = 0,
    Review = 1,
    Challenge = 2,
    DeclineRecommended = 3
}
