using AfriWallet.P2P.Directory.Persistence;
using AfriWallet.P2P.Infrastructure;

namespace IdentityService.Api.Identity;

public sealed class PublicAfWalIdReadService(IAfWalIdentityDirectory identities)
{
    public async Task<PublicAfWalIdReadModel?> ReadAsync(
        string afWalId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalized;
        try
        {
            normalized = RecipientDirectoryNormalization.NormalizeAfWalId(afWalId);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var ownerId = await identities.ResolveOwnerIdAsync(normalized, cancellationToken);
        return ownerId is null
            ? null
            : new PublicAfWalIdReadModel(normalized);
    }
}
