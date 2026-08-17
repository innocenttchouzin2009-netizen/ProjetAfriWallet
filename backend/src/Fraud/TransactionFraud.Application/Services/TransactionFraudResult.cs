using AfriWallet.Fraud.TransactionFraud.Domain.Detection;
using AfriWallet.Fraud.TransactionFraud.Domain.Factors;

namespace AfriWallet.Fraud.TransactionFraud.Application.Services;

public sealed record TransactionFraudResult(
    Guid DetectionId,
    Guid TransactionId,
    string Awid,
    int Score,
    FraudDetectionBand Band,
    FraudDetectionRecommendation Recommendation,
    IReadOnlyCollection<TransactionFraudFactor> Factors,
    DateTimeOffset DetectedAtUtc);
