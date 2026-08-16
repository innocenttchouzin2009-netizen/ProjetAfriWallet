using System.Collections.Concurrent;
using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

namespace AfriWallet.Fraud.TransactionFraud.Infrastructure;

public sealed class InMemoryTransactionFraudAuditStore : ITransactionFraudAuditStore
{
    private readonly ConcurrentQueue<TransactionFraudAuditEvent> _events = new();

    public Task AppendAsync(TransactionFraudAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<TransactionFraudAuditEvent>> GetByDetectionAsync(Guid detectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<TransactionFraudAuditEvent> result = _events
            .Where(x => x.DetectionId == detectionId)
            .ToArray();
        return Task.FromResult(result);
    }
}
