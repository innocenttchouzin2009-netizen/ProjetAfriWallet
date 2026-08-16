using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Application.Sessions;

public sealed record CreateVerificationCommand(
    Guid ComplianceProfileId,
    VerificationType Type,
    string ProviderCode,
    string IdempotencyKey,
    string Actor);

public sealed record CompleteVerificationCommand(
    Guid SessionId,
    bool Verified,
    string Code,
    string ProviderReference,
    string Actor);
