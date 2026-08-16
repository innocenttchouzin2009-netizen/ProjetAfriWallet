namespace AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

public sealed record VerificationResult(
    bool Verified,
    string Code,
    string ProviderReference,
    DateTimeOffset CompletedAtUtc);
