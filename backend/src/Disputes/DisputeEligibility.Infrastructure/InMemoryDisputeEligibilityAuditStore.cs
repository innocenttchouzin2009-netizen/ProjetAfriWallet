using System.Collections.Concurrent;
using AfriWallet.Disputes.Eligibility.Application.Abstractions;

namespace AfriWallet.Disputes.Eligibility.Infrastructure;

public sealed class InMemoryDisputeEligibilityAuditStore : IDisputeEligibilityAuditStore
{
    private readonly ConcurrentQueue<DisputeEligibilityAuditEvent> events = new();

    public Task AppendAsync(DisputeEligibilityAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DisputeEligibilityAuditEvent>> GetAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<DisputeEligibilityAuditEvent> result = events
            .Where(x => x.DecisionId == decisionId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}
