using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Lifecycle;

public sealed record RefreshRotationResult(
    RefreshRotationStatus Status,
    AuthSession? Session = null,
    string? RefreshToken = null
);
