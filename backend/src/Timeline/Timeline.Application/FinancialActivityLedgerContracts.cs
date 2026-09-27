using AfriWallet.Ledger.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Timeline.Application;

public interface IFinancialActivityWalletAccountResolver
{
    Task<AccountId?> ResolveAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default);
}
