using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IdentityService.Api.Auth.Persistence;

public sealed class EfAuthUserStore(AuthDbContext dbContext) : IAuthUserStore
{
    private readonly AuthDbContext _dbContext = dbContext;

    public async Task<AuthUser?> FindByNormalizedIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedIdentifier == normalizedIdentifier,
                cancellationToken);

        return entity is null
            ? null
            : new AuthUser(
                entity.Id,
                entity.NormalizedIdentifier,
                entity.PasswordHash,
                entity.IsDisabled,
                entity.CreatedAtUtc);
    }

    public async Task<bool> TryAddAsync(
        AuthUser user,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(new Entities.AuthUserEntity
        {
            Id = user.Id,
            NormalizedIdentifier = user.NormalizedIdentifier,
            PasswordHash = user.PasswordHash,
            IsDisabled = user.IsDisabled,
            CreatedAtUtc = user.CreatedAtUtc
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException
                  && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
