namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthUserEntity
{
    public Guid Id { get; set; }

    public string NormalizedIdentifier { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsDisabled { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
