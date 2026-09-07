using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Lifecycle;

public sealed record AuthSessionGrant(
    AuthSession Session,
    string RefreshToken
);
