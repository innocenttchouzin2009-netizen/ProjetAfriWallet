namespace AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;

public sealed record BankProviderHealth(
    string ProviderCode,
    bool Healthy,
    string Status,
    DateTime CheckedAtUtc);
