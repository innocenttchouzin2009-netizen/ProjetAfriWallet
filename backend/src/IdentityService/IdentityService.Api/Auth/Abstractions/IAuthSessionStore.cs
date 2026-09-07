using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Abstractions;

public interface IAuthSessionStore
{
    Task<AuthSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<AuthSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<AuthSessionStoreRotationResult> TryRotateRefreshTokenAsync(
        string currentRefreshTokenHash,
        string replacementRefreshTokenHash,
        DateTimeOffset rotatedAtUtc,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AuthSession session,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);
}
