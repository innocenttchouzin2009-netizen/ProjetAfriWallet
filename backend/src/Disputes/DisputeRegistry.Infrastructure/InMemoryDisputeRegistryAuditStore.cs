using System.Collections.Concurrent;
using AfriWallet.Disputes.Registry.Application.Abstractions;

namespace AfriWallet.Disputes.Registry.Infrastructure;

public sealed class InMemoryDisputeRegistryAuditStore : IDisputeRegistryAuditStore
{
    private readonly ConcurrentQueue<DisputeRegistryAuditEvent> events = new();

    public Task AppendAsync(DisputeRegistryAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DisputeRegistryAuditEvent>> GetByClaimAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<DisputeRegistryAuditEvent> result = events
            .Where(x => x.ClaimId == claimId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}
