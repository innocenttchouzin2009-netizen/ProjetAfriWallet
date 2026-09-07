namespace IdentityService.Api.Auth.Domain;

public sealed record AuthSession(
    Guid Id,
    Guid UserId,
    string DeviceId,
    string RefreshTokenHash,
    Guid RefreshTokenFamilyId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string? RevocationReason,
    long TokenVersion,
    AuthSessionStatus Status
);
