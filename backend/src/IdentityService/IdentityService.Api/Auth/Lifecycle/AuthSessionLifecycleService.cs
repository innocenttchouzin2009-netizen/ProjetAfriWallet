using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Lifecycle;

public sealed class AuthSessionLifecycleService(
    IAuthSessionStore sessionStore,
    IRefreshTokenService refreshTokenService,
    IClock clock,
    AuthSessionOptions options)
{
    public async Task<AuthSessionGrant> CreateAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var now = clock.UtcNow;
        var refreshToken = refreshTokenService.Generate();
        var session = new AuthSession(
            Guid.NewGuid(),
            userId,
            deviceId,
            refreshTokenService.Hash(refreshToken),
            Guid.NewGuid(),
            now,
            now,
            now.Add(options.SessionLifetime),
            null,
            null,
            1,
            AuthSessionStatus.Active);

        await sessionStore.SaveAsync(session, cancellationToken);
        return new AuthSessionGrant(session, refreshToken);
    }

    public async Task<RefreshRotationResult> RotateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var currentHash = refreshTokenService.Hash(refreshToken);
        var replacementToken = refreshTokenService.Generate();
        var replacementHash = refreshTokenService.Hash(replacementToken);
        var result = await sessionStore.TryRotateRefreshTokenAsync(
            currentHash,
            replacementHash,
            clock.UtcNow,
            cancellationToken);

        return result.Status == RefreshRotationStatus.Succeeded
            ? new RefreshRotationResult(result.Status, result.Session, replacementToken)
            : new RefreshRotationResult(result.Status, result.Session);
    }

    public Task RevokeAsync(
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default) =>
        sessionStore.RevokeAsync(sessionId, reason, clock.UtcNow, cancellationToken);

    public Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default) =>
        sessionStore.RevokeAllForUserAsync(userId, reason, clock.UtcNow, cancellationToken);
}
