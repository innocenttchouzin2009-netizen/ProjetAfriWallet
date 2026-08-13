using System.Collections.Concurrent;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;
using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Transfers;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Repositories;

public sealed class InMemoryProviderTransferRepository : IProviderTransferRepository
{
    private readonly ConcurrentDictionary<Guid, ProviderTransfer> _transfers = new();
    private readonly ConcurrentDictionary<string, Guid> _idempotencyIndex = new(StringComparer.Ordinal);

    public Task AddAsync(ProviderTransfer transfer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _transfers[transfer.ProviderTransferId] = transfer;
        _idempotencyIndex[transfer.IdempotencyKey] = transfer.ProviderTransferId;
        return Task.CompletedTask;
    }

    public Task<ProviderTransfer?> GetAsync(Guid providerTransferId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _transfers.TryGetValue(providerTransferId, out var transfer);
        return Task.FromResult(transfer);
    }

    public Task<ProviderTransfer?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_idempotencyIndex.TryGetValue(idempotencyKey, out var transferId))
            return Task.FromResult<ProviderTransfer?>(null);

        _transfers.TryGetValue(transferId, out var transfer);
        return Task.FromResult(transfer);
    }
}
