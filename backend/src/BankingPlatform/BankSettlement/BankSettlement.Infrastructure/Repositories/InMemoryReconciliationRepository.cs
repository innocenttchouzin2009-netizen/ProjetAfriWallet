using System.Collections.Concurrent;
using AfriWallet.BankingPlatform.BankSettlement.Application;
using AfriWallet.BankingPlatform.BankSettlement.Domain.Reconciliation;

namespace AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Repositories;

public sealed class InMemoryReconciliationRepository : IReconciliationRepository
{
    private readonly ConcurrentDictionary<Guid, ReconciliationRecord> _records = new();

    public Task<ReconciliationRecord?> GetByIdAsync(
        Guid reconciliationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _records.TryGetValue(reconciliationId, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyCollection<ReconciliationRecord>> GetForBatchAsync(
        Guid settlementBatchId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<ReconciliationRecord>>(
            _records.Values
                .Where(x => x.SettlementBatchId == settlementBatchId)
                .ToList());
    }

    public Task SaveAsync(
        ReconciliationRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _records[record.ReconciliationId] = record;
        return Task.CompletedTask;
    }
}
