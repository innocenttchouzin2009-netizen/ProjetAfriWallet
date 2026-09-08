using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Auth.Persistence;

public sealed class EfAuthUserStore(AuthDbContext db) : IAuthUserStore
{
    public async Task<AuthUser?> FindByNormalizedIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedIdentifier == normalizedIdentifier,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<bool> TryAddAsync(
        AuthUser user,
        CancellationToken cancellationToken = default)
    {
        var entity = new AuthUserEntity
        {
            Id = user.Id,
            NormalizedIdentifier = user.NormalizedIdentifier,
            PasswordHash = user.PasswordHash,
            IsDisabled = user.IsDisabled,
            CreatedAtUtc = user.CreatedAtUtc,
        };

        db.Users.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;

            var duplicateExists = await db.Users
                .AsNoTracking()
                .AnyAsync(
                    candidate => candidate.NormalizedIdentifier == user.NormalizedIdentifier,
                    cancellationToken);

            if (duplicateExists)
            {
                return false;
            }

            throw;
        }
    }

    private static AuthUser ToDomain(AuthUserEntity entity) => new(
        entity.Id,
        entity.NormalizedIdentifier,
        entity.PasswordHash,
        entity.IsDisabled,
        entity.CreatedAtUtc);
}
