using AfriWallet.Fraud.TransactionFraud.Domain.Factors;

namespace AfriWallet.Fraud.TransactionFraud.Domain.Detection;

public sealed class TransactionFraudDetection
{
    public TransactionFraudDetection(
        Guid detectionId,
        Guid transactionId,
        string awid,
        int score,
        FraudDetectionBand band,
        FraudDetectionRecommendation recommendation,
        IReadOnlyCollection<TransactionFraudFactor> factors,
        DateTimeOffset detectedAtUtc)
    {
        if (detectionId == Guid.Empty)
            throw new ArgumentException("Detection ID is required.");
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction ID is required.");
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.");

        DetectionId = detectionId;
        TransactionId = transactionId;
        Awid = awid.Trim();
        Score = Math.Clamp(score, 0, 100);
        Band = band;
        Recommendation = recommendation;
        Factors = factors;
        DetectedAtUtc = detectedAtUtc;
    }

    public Guid DetectionId { get; }
    public Guid TransactionId { get; }
    public string Awid { get; }
    public int Score { get; }
    public FraudDetectionBand Band { get; }
    public FraudDetectionRecommendation Recommendation { get; }
    public IReadOnlyCollection<TransactionFraudFactor> Factors { get; }
    public DateTimeOffset DetectedAtUtc { get; }
}
