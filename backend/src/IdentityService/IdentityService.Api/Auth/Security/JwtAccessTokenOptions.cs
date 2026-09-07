namespace IdentityService.Api.Auth.Security;

public sealed record JwtAccessTokenOptions(
    string Issuer,
    string Audience,
    string SigningKey
);
