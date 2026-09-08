using System.Data;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Auth.Persistence;

public sealed class EfAuthSessionStore(AuthDbContext db) : IAuthSessionStore
{
    public async Task<AuthSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<AuthSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                session => session.RefreshTokenHash == refreshTokenHash,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<AuthSessionStoreRotationResult> TryRotateRefreshTokenAsync(
        string currentRefreshTokenHash,
        string replacementRefreshTokenHash,
        DateTimeOffset rotatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var consumed = await db.ConsumedRefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.RefreshTokenHash == currentRefreshTokenHash,
                cancellationToken);

        if (consumed is not null)
        {
            var reusedSession = await db.Sessions
                .SingleOrDefaultAsync(session => session.Id == consumed.SessionId, cancellationToken);

            if (reusedSession is not null && reusedSession.Status != AuthSessionStatus.Revoked)
            {
                Revoke(reusedSession, rotatedAtUtc, "refresh_token_reuse");
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(
                RefreshRotationStatus.Reused,
                reusedSession is null ? null : ToDomain(reusedSession));
        }

        var current = await db.Sessions
            .SingleOrDefaultAsync(
                session => session.RefreshTokenHash == currentRefreshTokenHash,
                cancellationToken);

        if (current is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(RefreshRotationStatus.NotFound);
        }

        if (current.Status == AuthSessionStatus.Revoked)
        {
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(RefreshRotationStatus.Revoked, ToDomain(current));
        }

        if (current.Status == AuthSessionStatus.Expired || rotatedAtUtc >= current.ExpiresAtUtc)
        {
            current.Status = AuthSessionStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(RefreshRotationStatus.Expired, ToDomain(current));
        }

        db.ConsumedRefreshTokens.Add(new AuthConsumedRefreshTokenEntity
        {
            RefreshTokenHash = currentRefreshTokenHash,
            SessionId = current.Id,
            ConsumedAtUtc = rotatedAtUtc,
        });

        current.RefreshTokenHash = replacementRefreshTokenHash;
        current.LastSeenAtUtc = rotatedAtUtc;
        current.TokenVersion = checked(current.TokenVersion + 1);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AuthSessionStoreRotationResult(RefreshRotationStatus.Succeeded, ToDomain(current));
    }

    public async Task SaveAsync(
        AuthSession session,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Sessions
            .SingleOrDefaultAsync(candidate => candidate.Id == session.Id, cancellationToken);

        if (entity is null)
        {
            db.Sessions.Add(ToEntity(session));
        }
        else
        {
            Apply(session, entity);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Sessions
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        if (entity is null || entity.Status == AuthSessionStatus.Revoked)
        {
            return;
        }

        Revoke(entity, DateTimeOffset.UtcNow, reason);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var sessions = await db.Sessions
            .Where(session => session.UserId == userId && session.Status != AuthSessionStatus.Revoked)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return;
        }

        var revokedAtUtc = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            Revoke(session, revokedAtUtc, reason);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Revoke(
        AuthSessionEntity entity,
        DateTimeOffset revokedAtUtc,
        string reason)
    {
        entity.Status = AuthSessionStatus.Revoked;
        entity.RevokedAtUtc = revokedAtUtc;
        entity.RevocationReason = reason;
    }

    private static AuthSessionEntity ToEntity(AuthSession session) => new()
    {
        Id = session.Id,
        UserId = session.UserId,
        DeviceId = session.DeviceId,
        RefreshTokenHash = session.RefreshTokenHash,
        RefreshTokenFamilyId = session.RefreshTokenFamilyId,
        CreatedAtUtc = session.CreatedAtUtc,
        LastSeenAtUtc = session.LastSeenAtUtc,
        ExpiresAtUtc = session.ExpiresAtUtc,
        RevokedAtUtc = session.RevokedAtUtc,
        RevocationReason = session.RevocationReason,
        TokenVersion = session.TokenVersion,
        Status = session.Status,
    };

    private static void Apply(AuthSession session, AuthSessionEntity entity)
    {
        entity.UserId = session.UserId;
        entity.DeviceId = session.DeviceId;
        entity.RefreshTokenHash = session.RefreshTokenHash;
        entity.RefreshTokenFamilyId = session.RefreshTokenFamilyId;
        entity.CreatedAtUtc = session.CreatedAtUtc;
        entity.LastSeenAtUtc = session.LastSeenAtUtc;
        entity.ExpiresAtUtc = session.ExpiresAtUtc;
        entity.RevokedAtUtc = session.RevokedAtUtc;
        entity.RevocationReason = session.RevocationReason;
        entity.TokenVersion = session.TokenVersion;
        entity.Status = session.Status;
    }

    private static AuthSession ToDomain(AuthSessionEntity entity) => new(
        entity.Id,
        entity.UserId,
        entity.DeviceId,
        entity.RefreshTokenHash,
        entity.RefreshTokenFamilyId,
        entity.CreatedAtUtc,
        entity.LastSeenAtUtc,
        entity.ExpiresAtUtc,
        entity.RevokedAtUtc,
        entity.RevocationReason,
        entity.TokenVersion,
        entity.Status);
}
