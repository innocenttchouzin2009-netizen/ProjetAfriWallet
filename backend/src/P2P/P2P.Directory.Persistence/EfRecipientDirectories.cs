using AfriWallet.P2P.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.P2P.Directory.Persistence;

public sealed class EfAfWalIdentityDirectory(RecipientDirectoryDbContext dbContext) : IAfWalIdentityDirectory
{
    public async Task<Guid?> ResolveOwnerIdAsync(string afWalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = RecipientDirectoryNormalization.NormalizeAfWalId(afWalId);
        return await dbContext.AfWalIdentities
            .AsNoTracking()
            .Where(x => x.IsActive && x.AfWalId == normalized)
            .Select(x => (Guid?)x.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed class EfQrRecipientDirectory(RecipientDirectoryDbContext dbContext) : IQrRecipientDirectory
{
    public async Task<Guid?> ResolveOwnerIdAsync(string qrToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tokenHash = RecipientDirectoryNormalization.HashQrToken(qrToken);
        return await dbContext.QrRecipients
            .AsNoTracking()
            .Where(x => x.IsActive && x.TokenHash == tokenHash)
            .Select(x => (Guid?)x.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
