namespace IdentityService.Api.Auth.Contracts;

public sealed record LoginRequest(
    string Identifier,
    string Password,
    string DeviceId,
    string Platform,
    string? DeviceName
);
