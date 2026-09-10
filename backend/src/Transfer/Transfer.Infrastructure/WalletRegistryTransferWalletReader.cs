using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class WalletRegistryTransferWalletReader(
    IWalletRepository walletRepository,
    IWalletLedgerAccountResolver accountResolver) : ITransferWalletReader
{
    public async Task<TransferWalletSnapshot?> GetAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        if (walletId == Guid.Empty)
        {
            throw new ArgumentException("Wallet id cannot be empty.", nameof(walletId));
        }

        var wallet = await walletRepository.GetAsync(WalletId.From(walletId), cancellationToken);
        if (wallet is null)
        {
            return null;
        }

        var accountId = await accountResolver.ResolveAsync(walletId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet has no resolved Ledger account.");

        return new TransferWalletSnapshot(
            wallet.Id.Value,
            accountId,
            wallet.Currency.Code,
            wallet.Status == WalletStatus.Active);
    }
}
