using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Lifecycle;

public sealed class InMemoryAuthSessionStore : IAuthSessionStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, AuthSession> _sessions = [];
    private readonly Dictionary<string, Guid> _currentRefreshTokens = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _consumedRefreshTokens = new(StringComparer.Ordinal);

    public Task<AuthSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }
    }

    public Task<AuthSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_currentRefreshTokens.TryGetValue(refreshTokenHash, out var sessionId)
                && _sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult<AuthSession?>(session);
            }

            return Task.FromResult<AuthSession?>(null);
        }
    }

    public Task<AuthSessionStoreRotationResult> TryRotateRefreshTokenAsync(
        string currentRefreshTokenHash,
        string replacementRefreshTokenHash,
        DateTimeOffset rotatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_consumedRefreshTokens.TryGetValue(currentRefreshTokenHash, out var reusedSessionId))
            {
                if (_sessions.TryGetValue(reusedSessionId, out var reusedSession))
                {
                    var revoked = RevokeCore(reusedSession, rotatedAtUtc, "refresh_token_reuse");
                    return Task.FromResult(new AuthSessionStoreRotationResult(RefreshRotationStatus.Reused, revoked));
                }

                return Task.FromResult(new AuthSessionStoreRotationResult(RefreshRotationStatus.Reused));
            }

            if (!_currentRefreshTokens.TryGetValue(currentRefreshTokenHash, out var sessionId)
                || !_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(new AuthSessionStoreRotationResult(RefreshRotationStatus.NotFound));
            }

            if (session.Status == AuthSessionStatus.Revoked)
            {
                return Task.FromResult(new AuthSessionStoreRotationResult(RefreshRotationStatus.Revoked, session));
            }

            if (session.Status == AuthSessionStatus.Expired || rotatedAtUtc >= session.ExpiresAtUtc)
            {
                var expired = session with { Status = AuthSessionStatus.Expired };
                _sessions[session.Id] = expired;
                return Task.FromResult(new AuthSessionStoreRotationResult(RefreshRotationStatus.Expired, expired));
            }

            _currentRefreshTokens.Remove(currentRefreshTokenHash);
            _consumedRefreshTokens[currentRefreshTokenHash] = session.Id;

            var rotated = session with
            {
                RefreshTokenHash = replacementRefreshTokenHash,
                LastSeenAtUtc = rotatedAtUtc,
                TokenVersion = checked(session.TokenVersion + 1)
            };

            _sessions[session.Id] = rotated;
            _currentRefreshTokens[replacementRefreshTokenHash] = session.Id;

            return Task.FromResult(new AuthSessionStoreRotationResult(RefreshRotationStatus.Succeeded, rotated));
        }
    }

    public Task SaveAsync(
        AuthSession session,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _sessions[session.Id] = session;
            _currentRefreshTokens[session.RefreshTokenHash] = session.Id;
        }

        return Task.CompletedTask;
    }

    public Task RevokeAsync(
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                RevokeCore(session, DateTimeOffset.UtcNow, reason);
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var sessions = _sessions.Values.Where(session => session.UserId == userId).ToArray();
            foreach (var session in sessions)
            {
                RevokeCore(session, DateTimeOffset.UtcNow, reason);
            }
        }

        return Task.CompletedTask;
    }

    private AuthSession RevokeCore(AuthSession session, DateTimeOffset revokedAtUtc, string reason)
    {
        if (session.Status == AuthSessionStatus.Revoked)
        {
            return session;
        }

        var revoked = session with
        {
            Status = AuthSessionStatus.Revoked,
            RevokedAtUtc = revokedAtUtc,
            RevocationReason = reason
        };
        _sessions[session.Id] = revoked;
        return revoked;
    }
}
