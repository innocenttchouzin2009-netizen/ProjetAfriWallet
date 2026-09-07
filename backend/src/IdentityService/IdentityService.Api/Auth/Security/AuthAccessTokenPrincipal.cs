namespace IdentityService.Api.Auth.Security;

public sealed record AuthAccessTokenPrincipal(
    Guid UserId,
    Guid SessionId,
    long TokenVersion
);
