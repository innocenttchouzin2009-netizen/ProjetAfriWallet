using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Application.Abstractions;

public interface ITransactionHistoryOwnedWalletReader
{
    Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
