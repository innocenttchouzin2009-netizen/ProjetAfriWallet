namespace IdentityService.Api.Auth.Contracts;

public sealed record AuthErrorResponse(
    string Code,
    string Message,
    string? TraceId = null
);
