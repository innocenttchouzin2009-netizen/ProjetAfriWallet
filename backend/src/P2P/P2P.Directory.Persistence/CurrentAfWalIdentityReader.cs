using Microsoft.EntityFrameworkCore;

namespace AfriWallet.P2P.Directory.Persistence;

public sealed record CurrentAfWalIdentity(Guid UserId, string AfWalId);

public interface ICurrentAfWalIdentityReader
{
    Task<CurrentAfWalIdentity?> FindByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

public sealed class EfCurrentAfWalIdentityReader(RecipientDirectoryDbContext dbContext)
    : ICurrentAfWalIdentityReader
{
    public async Task<CurrentAfWalIdentity?> FindByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner id is required.", nameof(ownerId));
        }

        return await dbContext.AfWalIdentities
            .AsNoTracking()
            .Where(entry => entry.OwnerId == ownerId && entry.IsActive)
            .Select(entry => new CurrentAfWalIdentity(entry.OwnerId, entry.AfWalId))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
