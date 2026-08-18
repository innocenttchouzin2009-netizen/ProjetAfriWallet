using System.Collections.Concurrent;
using AfriWallet.Disputes.Resolution.Application.Abstractions;

namespace AfriWallet.Disputes.Resolution.Infrastructure;

public sealed class InMemoryResolutionAuditStore : IResolutionAuditStore
{
    private readonly ConcurrentQueue<ResolutionAuditEvent> events = new();

    public Task AppendAsync(ResolutionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ResolutionAuditEvent>> GetAsync(Guid resolutionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ResolutionAuditEvent> result = events.Where(x => x.ResolutionId == resolutionId).ToArray();
        return Task.FromResult(result);
    }
}
