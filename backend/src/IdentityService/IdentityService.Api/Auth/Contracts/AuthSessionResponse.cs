namespace IdentityService.Api.Auth.Contracts;

public sealed record AuthSessionResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    long ExpiresIn,
    Guid SessionId,
    Guid UserId
);
