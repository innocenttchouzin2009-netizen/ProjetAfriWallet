namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthConsumedRefreshTokenEntity
{
    public string RefreshTokenHash { get; set; } = string.Empty;

    public Guid SessionId { get; set; }

    public DateTimeOffset ConsumedAtUtc { get; set; }
}
