using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Infrastructure;

public sealed class WalletRepositoryTransactionHistoryOwnedWalletReader(
    IWalletRepository walletRepository) : ITransactionHistoryOwnedWalletReader
{
    public async Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var wallets = await walletRepository.ListByOwnerAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet repository returned null.");

        return wallets
            .Where(wallet => wallet.OwnerId == userId && wallet.Id.Value != Guid.Empty)
            .Select(wallet => wallet.Id)
            .Distinct()
            .OrderBy(walletId => walletId.Value)
            .ToArray();
    }
}
