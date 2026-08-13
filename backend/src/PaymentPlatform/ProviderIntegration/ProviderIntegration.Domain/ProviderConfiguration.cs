namespace AfriWallet.PaymentPlatform.ProviderIntegration.Domain;

public sealed record ProviderConfiguration(
    string ProviderCode,
    ProviderEnvironment Environment,
    Uri BaseUri,
    string CredentialKey,
    string WebhookSecretKey,
    TimeSpan Timeout,
    int MaxRetries,
    bool Enabled);