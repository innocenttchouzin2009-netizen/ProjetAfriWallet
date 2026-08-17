namespace AfriWallet.Fraud.Decision.Domain.Inputs;

public sealed record DeviceRiskInput(
    string Awid,
    string DeviceId,
    int Score,
    string Band,
    string Recommendation,
    DateTimeOffset CalculatedAtUtc);