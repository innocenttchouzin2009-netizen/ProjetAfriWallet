using System.Collections.Concurrent;
using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

namespace AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

public sealed class InMemoryMonitoringAuditStore : IMonitoringAuditStore
{
    private readonly ConcurrentQueue<MonitoringAuditEvent> _events = new();

    public Task AppendAsync(
        MonitoringAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<MonitoringAuditEvent>> GetByTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<MonitoringAuditEvent> result = _events
            .Where(auditEvent => auditEvent.TransactionId == transactionId)
            .ToArray();
        return Task.FromResult(result);
    }
}