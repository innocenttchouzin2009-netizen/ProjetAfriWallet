namespace IdentityService.Api.Auth.Persistence.Entities;

public sealed class ConsumedRefreshTokenEntity
{
    public required string RefreshTokenHash { get; set; }
    public Guid SessionId { get; set; }
    public Guid RefreshTokenFamilyId { get; set; }
    public DateTimeOffset ConsumedAtUtc { get; set; }
}
