using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Application.Sessions;

public sealed record VerificationSessionResult(
    Guid Id,
    Guid ComplianceProfileId,
    VerificationType Type,
    string ProviderCode,
    VerificationStatus Status,
    string? ProviderReference,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
