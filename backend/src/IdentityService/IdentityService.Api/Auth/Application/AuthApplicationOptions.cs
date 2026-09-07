namespace IdentityService.Api.Auth.Application;

public sealed record AuthApplicationOptions(TimeSpan AccessTokenLifetime)
{
    public static AuthApplicationOptions Default { get; } = new(TimeSpan.FromMinutes(15));
}
