namespace AfriWallet.PaymentPlatform.ProviderIntegration.Domain;

public sealed record ProviderError(
    string ProviderCode,
    string Code,
    string Message,
    bool Retryable,
    string? ProviderReference = null);