namespace AfriWallet.Fraud.Decision.Domain.Inputs;

public sealed record TransactionFraudInput(
    Guid TransactionId,
    string Awid,
    int Score,
    string Band,
    string Recommendation,
    DateTimeOffset DetectedAtUtc);