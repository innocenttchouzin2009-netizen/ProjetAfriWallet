using System.Collections.Concurrent;
using AfriWallet.Fraud.Intelligence.Application.Abstractions;

namespace AfriWallet.Fraud.Intelligence.Infrastructure;

public sealed class InMemoryFraudIntelligenceAuditStore : IFraudIntelligenceAuditStore
{
    private readonly ConcurrentQueue<FraudIntelligenceAuditEvent> events = new();

    public Task AppendAsync(FraudIntelligenceAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<FraudIntelligenceAuditEvent>> GetAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<FraudIntelligenceAuditEvent> result = events.Where(x => x.FindingId == findingId).ToArray();
        return Task.FromResult(result);
    }
}