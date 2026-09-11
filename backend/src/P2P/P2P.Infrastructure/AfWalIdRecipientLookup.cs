using AfriWallet.P2P.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Infrastructure;

public sealed class AfWalIdRecipientLookup(
    IAfWalIdentityDirectory identityDirectory,
    WalletRecipientSelector walletSelector) : IAfWalIdRecipientLookup
{
    public async Task<WalletId?> ResolveAsync(
        string afWalId,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(afWalId))
        {
            throw new ArgumentException("AfWal ID is required.", nameof(afWalId));
        }

        ArgumentNullException.ThrowIfNull(currency);
        cancellationToken.ThrowIfCancellationRequested();

        var ownerId = await identityDirectory.ResolveOwnerIdAsync(afWalId, cancellationToken);
        if (ownerId is null)
        {
            return null;
        }

        return await walletSelector.SelectActiveAsync(ownerId.Value, currency, cancellationToken);
    }
}
