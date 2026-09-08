namespace IdentityService.Api.Auth.Persistence.Entities;

public sealed class AuthUserEntity
{
    public Guid Id { get; set; }
    public required string NormalizedIdentifier { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsDisabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
