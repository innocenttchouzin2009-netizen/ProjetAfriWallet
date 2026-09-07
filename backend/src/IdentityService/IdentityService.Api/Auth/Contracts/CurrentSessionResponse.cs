namespace IdentityService.Api.Auth.Contracts;

public sealed record CurrentSessionResponse(
    Guid UserId,
    Guid SessionId,
    string DeviceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    long TokenVersion);
