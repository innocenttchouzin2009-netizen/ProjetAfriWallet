using System.Collections.Concurrent;
using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

public sealed class InMemoryTransactionHistoryRepository : ITransactionHistoryRepository
{
    private readonly ConcurrentQueue<MonitoredTransaction> _transactions = new();

    public Task AddAsync(
        MonitoredTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _transactions.Enqueue(transaction);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<MonitoredTransaction>> GetByAwidAsync(
        string awid,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<MonitoredTransaction> result = _transactions
            .Where(transaction =>
                string.Equals(transaction.Awid, awid, StringComparison.OrdinalIgnoreCase) &&
                transaction.OccurredAtUtc >= fromUtc)
            .OrderBy(transaction => transaction.OccurredAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}