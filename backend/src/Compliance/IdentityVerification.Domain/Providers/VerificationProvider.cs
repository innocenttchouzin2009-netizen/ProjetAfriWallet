namespace AfriWallet.Compliance.IdentityVerification.Domain.Providers;

public sealed record VerificationProvider(
    string Code,
    string DisplayName,
    bool Sandbox,
    ProviderStatus Status);
