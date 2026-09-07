namespace IdentityService.Api.Auth.Contracts;

public sealed record RegisterRequest(
    string Identifier,
    string Password
);
