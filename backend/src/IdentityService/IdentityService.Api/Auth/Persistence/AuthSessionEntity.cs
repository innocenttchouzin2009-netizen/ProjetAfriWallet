using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthSessionEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string RefreshTokenHash { get; set; } = string.Empty;

    public Guid RefreshTokenFamilyId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset LastSeenAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? RevocationReason { get; set; }

    public long TokenVersion { get; set; }

    public AuthSessionStatus Status { get; set; }
}
