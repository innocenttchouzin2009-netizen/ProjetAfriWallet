namespace AfriWallet.Fraud.TransactionFraud.Domain.Factors;

public sealed record TransactionFraudFactor(
    TransactionFraudFactorType Type,
    int Score,
    string Reason,
    string? EvidenceId);
