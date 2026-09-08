using System.Data;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Auth.Persistence;

public sealed class EfAuthSessionStore(AuthDbContext dbContext) : IAuthSessionStore
{
    private readonly AuthDbContext _dbContext = dbContext;

    public async Task<AuthSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<AuthSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sessions
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
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var consumed = await _dbContext.ConsumedRefreshTokens
            .SingleOrDefaultAsync(
                token => token.RefreshTokenHash == currentRefreshTokenHash,
                cancellationToken);

        if (consumed is not null)
        {
            var reusedSession = await _dbContext.Sessions
                .SingleOrDefaultAsync(session => session.Id == consumed.SessionId, cancellationToken);

            if (reusedSession is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new AuthSessionStoreRotationResult(RefreshRotationStatus.Reused);
            }

            Revoke(reusedSession, rotatedAtUtc, "refresh_token_reuse");
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(
                RefreshRotationStatus.Reused,
                ToDomain(reusedSession));
        }

        var sessionEntity = await _dbContext.Sessions
            .SingleOrDefaultAsync(
                session => session.RefreshTokenHash == currentRefreshTokenHash,
                cancellationToken);

        if (sessionEntity is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(RefreshRotationStatus.NotFound);
        }

        if (sessionEntity.Status == AuthSessionStatus.Revoked)
        {
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(
                RefreshRotationStatus.Revoked,
                ToDomain(sessionEntity));
        }

        if (sessionEntity.Status == AuthSessionStatus.Expired || rotatedAtUtc >= sessionEntity.ExpiresAtUtc)
        {
            sessionEntity.Status = AuthSessionStatus.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AuthSessionStoreRotationResult(
                RefreshRotationStatus.Expired,
                ToDomain(sessionEntity));
        }

        _dbContext.ConsumedRefreshTokens.Add(new ConsumedRefreshTokenEntity
        {
            RefreshTokenHash = currentRefreshTokenHash,
            SessionId = sessionEntity.Id,
            RefreshTokenFamilyId = sessionEntity.RefreshTokenFamilyId,
            ConsumedAtUtc = rotatedAtUtc
        });

        sessionEntity.RefreshTokenHash = replacementRefreshTokenHash;
        sessionEntity.LastSeenAtUtc = rotatedAtUtc;
        sessionEntity.TokenVersion = checked(sessionEntity.TokenVersion + 1);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AuthSessionStoreRotationResult(
            RefreshRotationStatus.Succeeded,
            ToDomain(sessionEntity));
    }

    public async Task SaveAsync(
        AuthSession session,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sessions
            .SingleOrDefaultAsync(existing => existing.Id == session.Id, cancellationToken);

        if (entity is null)
        {
            _dbContext.Sessions.Add(ToEntity(session));
        }
        else
        {
            Apply(session, entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sessions
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        Revoke(entity, DateTimeOffset.UtcNow, reason);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Sessions
            .Where(session => session.UserId == userId)
            .ToListAsync(cancellationToken);

        var revokedAtUtc = DateTimeOffset.UtcNow;
        foreach (var entity in entities)
        {
            Revoke(entity, revokedAtUtc, reason);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Revoke(
        AuthSessionEntity entity,
        DateTimeOffset revokedAtUtc,
        string reason)
    {
        if (entity.Status == AuthSessionStatus.Revoked)
        {
            return;
        }

        entity.Status = AuthSessionStatus.Revoked;
        entity.RevokedAtUtc = revokedAtUtc;
        entity.RevocationReason = reason;
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

    private static AuthSessionEntity ToEntity(AuthSession session)
    {
        var entity = new AuthSessionEntity
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
            Status = session.Status
        };

        return entity;
    }

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
}
