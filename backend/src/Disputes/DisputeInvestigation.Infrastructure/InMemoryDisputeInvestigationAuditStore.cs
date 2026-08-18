using System.Collections.Concurrent;
using AfriWallet.Disputes.Investigation.Application.Abstractions;

namespace AfriWallet.Disputes.Investigation.Infrastructure;

public sealed class InMemoryDisputeInvestigationAuditStore : IDisputeInvestigationAuditStore
{
    private readonly ConcurrentQueue<DisputeInvestigationAuditEvent> events = new();

    public Task AppendAsync(DisputeInvestigationAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DisputeInvestigationAuditEvent>> GetAsync(Guid investigationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<DisputeInvestigationAuditEvent> result = events
            .Where(x => x.InvestigationId == investigationId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}
