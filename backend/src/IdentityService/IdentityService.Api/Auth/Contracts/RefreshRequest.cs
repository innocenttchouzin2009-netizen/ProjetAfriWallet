namespace IdentityService.Api.Auth.Contracts;

public sealed record RefreshRequest(
    string RefreshToken
);
