using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Persistence.Entities;

public sealed class AuthSessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string DeviceId { get; set; }
    public required string RefreshTokenHash { get; set; }
    public Guid RefreshTokenFamilyId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public long TokenVersion { get; set; }
    public AuthSessionStatus Status { get; set; }
}
