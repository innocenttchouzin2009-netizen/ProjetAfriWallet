namespace IdentityService.Api.Auth.Domain;

public sealed record AuthSessionStoreRotationResult(
    RefreshRotationStatus Status,
    AuthSession? Session = null
);
