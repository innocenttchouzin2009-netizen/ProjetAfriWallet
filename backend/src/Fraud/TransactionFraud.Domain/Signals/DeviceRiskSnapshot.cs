namespace AfriWallet.Fraud.TransactionFraud.Domain.Signals;

public sealed record DeviceRiskSnapshot(
    string Awid,
    string DeviceId,
    int Score,
    string Band,
    string Recommendation,
    DateTimeOffset CalculatedAtUtc);
