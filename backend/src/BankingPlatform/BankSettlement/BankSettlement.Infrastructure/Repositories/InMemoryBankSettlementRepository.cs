using System.Collections.Concurrent;
using AfriWallet.BankingPlatform.BankSettlement.Application;
using AfriWallet.BankingPlatform.BankSettlement.Domain.Settlements;

namespace AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Repositories;

public sealed class InMemoryBankSettlementRepository : IBankSettlementRepository
{
    private readonly ConcurrentDictionary<Guid, BankSettlementBatch> _batches = new();
    private readonly ConcurrentDictionary<string, Guid> _idempotencyIndex = new(StringComparer.Ordinal);

    public Task<BankSettlementBatch?> GetByIdAsync(
        Guid settlementBatchId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _batches.TryGetValue(settlementBatchId, out var batch);
        return Task.FromResult(batch);
    }

    public Task<BankSettlementBatch?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_idempotencyIndex.TryGetValue(idempotencyKey, out var batchId))
            return Task.FromResult<BankSettlementBatch?>(null);

        _batches.TryGetValue(batchId, out var batch);
        return Task.FromResult(batch);
    }

    public Task SaveAsync(
        BankSettlementBatch batch,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _batches[batch.SettlementBatchId] = batch;
        _idempotencyIndex[batch.IdempotencyKey] = batch.SettlementBatchId;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<BankSettlementBatch>> GetOpenBatchesAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<BankSettlementBatch>>(
            _batches.Values
                .Where(x => x.Status == BankSettlementStatus.Open)
                .ToList());
    }
}
