namespace IdentityService.Api.Auth.Application;

public sealed record AuthUser(
    Guid Id,
    string NormalizedIdentifier,
    string PasswordHash,
    bool IsDisabled,
    DateTimeOffset CreatedAtUtc);
