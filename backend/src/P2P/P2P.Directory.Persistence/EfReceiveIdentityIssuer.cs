using System.Security.Cryptography;
using AfriWallet.P2P.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.P2P.Directory.Persistence;

public sealed class EfReceiveIdentityIssuer(RecipientDirectoryDbContext dbContext) : IReceiveIdentityIssuer
{
    public async Task<IssuedReceiveIdentity?> IssueAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner id is required.", nameof(ownerId));
        }

        var afWalId = await dbContext.AfWalIdentities
            .AsNoTracking()
            .Where(x => x.IsActive && x.OwnerId == ownerId)
            .Select(x => x.AfWalId)
            .SingleOrDefaultAsync(cancellationToken);

        if (afWalId is null)
        {
            return null;
        }

        var qrToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = RecipientDirectoryNormalization.HashQrToken(qrToken);

        var activeQrEntries = await dbContext.QrRecipients
            .Where(x => x.OwnerId == ownerId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var entry in activeQrEntries)
        {
            entry.IsActive = false;
        }

        dbContext.QrRecipients.Add(new QrRecipientEntry
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            TokenHash = tokenHash,
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new IssuedReceiveIdentity(afWalId, qrToken);
    }
}
