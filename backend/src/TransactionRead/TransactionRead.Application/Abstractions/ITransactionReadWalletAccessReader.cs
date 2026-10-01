using AfriWallet.Ledger.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionRead.Application.Abstractions;

public interface ITransactionReadWalletAccessReader
{
    Task<AccountId?> ResolveOwnedLedgerAccountAsync(
        Guid authenticatedUserId,
        WalletId walletId,
        CancellationToken cancellationToken = default);
}
