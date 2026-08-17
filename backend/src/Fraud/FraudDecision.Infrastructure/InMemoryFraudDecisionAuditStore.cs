using System.Collections.Concurrent;
using AfriWallet.Fraud.Decision.Application.Abstractions;

namespace AfriWallet.Fraud.Decision.Infrastructure;

public sealed class InMemoryFraudDecisionAuditStore : IFraudDecisionAuditStore
{
    private readonly ConcurrentQueue<FraudDecisionAuditEvent> events = new();

    public Task AppendAsync(FraudDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<FraudDecisionAuditEvent>> GetByDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<FraudDecisionAuditEvent> result = events.Where(x => x.DecisionId == decisionId).ToArray();
        return Task.FromResult(result);
    }
}