namespace AfriWallet.PaymentPlatform.ProviderIntegration.Domain;

public sealed record ProviderHealth(
    string ProviderCode,
    bool Available,
    double SuccessRate,
    double AverageLatencyMs,
    DateTimeOffset CheckedAt);