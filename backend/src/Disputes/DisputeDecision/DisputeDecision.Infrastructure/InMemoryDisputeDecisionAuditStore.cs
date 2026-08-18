using System.Collections.Concurrent;
using AfriWallet.Disputes.Decision.Application.Abstractions;

namespace AfriWallet.Disputes.Decision.Infrastructure;

public sealed class InMemoryDisputeDecisionAuditStore : IDisputeDecisionAuditStore
{
    private readonly ConcurrentQueue<DisputeDecisionAuditEvent> events = new();

    public Task AppendAsync(DisputeDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DisputeDecisionAuditEvent>> GetAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<DisputeDecisionAuditEvent> result = events
            .Where(x => x.DecisionId == decisionId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}
