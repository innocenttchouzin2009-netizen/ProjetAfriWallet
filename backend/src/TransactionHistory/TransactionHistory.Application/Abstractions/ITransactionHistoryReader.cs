using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Application.Abstractions;

public interface ITransactionHistoryReader
{
    Task<TransactionHistoryPage> ReadAsync(
        IReadOnlyCollection<WalletId> walletIds,
        TransactionHistoryPageRequest page,
        CancellationToken cancellationToken = default);
}
