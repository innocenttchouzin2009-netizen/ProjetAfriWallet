using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Transfers;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

public interface IProviderTransferRepository
{
    Task AddAsync(ProviderTransfer transfer, CancellationToken cancellationToken);
    Task<ProviderTransfer?> GetAsync(Guid providerTransferId, CancellationToken cancellationToken);
    Task<ProviderTransfer?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
}
