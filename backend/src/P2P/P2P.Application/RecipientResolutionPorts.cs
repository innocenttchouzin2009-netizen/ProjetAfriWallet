using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Application;

public interface IAfWalIdRecipientLookup
{
    Task<WalletId?> ResolveAsync(string afWalId, Currency currency, CancellationToken cancellationToken = default);
}

public interface IQrRecipientLookup
{
    Task<WalletId?> ResolveAsync(string qrToken, Currency currency, CancellationToken cancellationToken = default);
}
