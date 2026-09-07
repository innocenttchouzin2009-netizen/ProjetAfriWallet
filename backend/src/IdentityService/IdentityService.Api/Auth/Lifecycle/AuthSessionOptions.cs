namespace IdentityService.Api.Auth.Lifecycle;

public sealed record AuthSessionOptions(TimeSpan SessionLifetime)
{
    public static AuthSessionOptions Default { get; } = new(TimeSpan.FromDays(30));
}
