using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Infrastructure;

public sealed class WalletRecipientSelector(IWalletRepository walletRepository)
{
    public async Task<WalletId?> SelectActiveAsync(
        Guid ownerId,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            throw new InvalidOperationException("Recipient directory returned an empty owner id.");
        }

        ArgumentNullException.ThrowIfNull(currency);
        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await walletRepository.ListByOwnerAsync(ownerId, cancellationToken);
        var matching = wallets
            .Where(wallet => wallet.Status == WalletStatus.Active &&
                             string.Equals(wallet.Currency.Code, currency.Code, StringComparison.Ordinal))
            .ToArray();

        return matching.Length switch
        {
            0 => null,
            1 => matching[0].Id,
            _ => throw new InvalidOperationException("Recipient has multiple active wallets for the requested currency.")
        };
    }
}
