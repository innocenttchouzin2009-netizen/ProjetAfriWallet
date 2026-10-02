using AfriWallet.Ledger.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Application.Abstractions;

public interface ITransactionHistoryLedgerAccountResolver
{
    Task<AccountId?> ResolveAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default);
}
