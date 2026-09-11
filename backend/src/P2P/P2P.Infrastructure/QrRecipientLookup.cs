using AfriWallet.P2P.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Infrastructure;

public sealed class QrRecipientLookup(
    IQrRecipientDirectory qrDirectory,
    WalletRecipientSelector walletSelector) : IQrRecipientLookup
{
    public async Task<WalletId?> ResolveAsync(
        string qrToken,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            throw new ArgumentException("QR recipient token is required.", nameof(qrToken));
        }

        ArgumentNullException.ThrowIfNull(currency);
        cancellationToken.ThrowIfCancellationRequested();

        var ownerId = await qrDirectory.ResolveOwnerIdAsync(qrToken, cancellationToken);
        if (ownerId is null)
        {
            return null;
        }

        return await walletSelector.SelectActiveAsync(ownerId.Value, currency, cancellationToken);
    }
}
