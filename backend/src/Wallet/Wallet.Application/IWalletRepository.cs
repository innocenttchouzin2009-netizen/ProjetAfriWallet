using AfriWallet.Wallet.Domain;

namespace AfriWallet.Wallet.Application;

public interface IWalletRepository
{
    Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Wallet wallet, CancellationToken cancellationToken = default);
    Task<Domain.Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Wallet wallet, CancellationToken cancellationToken = default);
}
