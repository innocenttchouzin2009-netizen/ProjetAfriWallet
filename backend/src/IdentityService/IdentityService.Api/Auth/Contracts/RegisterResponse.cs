namespace IdentityService.Api.Auth.Contracts;

public sealed record RegisterResponse(
    Guid UserId,
    string Status
);
