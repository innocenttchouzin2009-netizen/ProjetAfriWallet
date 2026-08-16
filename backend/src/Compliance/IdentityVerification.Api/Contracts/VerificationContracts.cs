using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Api.Contracts;

public sealed record CreateVerificationRequest(
    Guid ComplianceProfileId,
    VerificationType Type,
    string ProviderCode,
    string IdempotencyKey);

public sealed record CompleteVerificationRequest(
    bool Verified,
    string Code,
    string ProviderReference);
