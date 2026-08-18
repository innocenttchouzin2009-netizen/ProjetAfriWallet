using System.Collections.Concurrent;
using AfriWallet.Disputes.Intelligence.Application.Abstractions;

namespace AfriWallet.Disputes.Intelligence.Infrastructure;

public sealed class InMemoryDisputeIntelligenceAuditStore : IDisputeIntelligenceAuditStore
{
    private readonly ConcurrentQueue<DisputeIntelligenceAuditEvent> events = new();

    public Task AppendAsync(DisputeIntelligenceAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DisputeIntelligenceAuditEvent>> GetAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<DisputeIntelligenceAuditEvent> result = events.Where(x => x.FindingId == findingId).ToArray();
        return Task.FromResult(result);
    }
}
