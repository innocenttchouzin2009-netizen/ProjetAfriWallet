using System.Collections.Concurrent;
using AfriWallet.Fraud.Investigation.Application.Abstractions;

namespace AfriWallet.Fraud.Investigation.Infrastructure;

public sealed class InMemoryFraudInvestigationAuditStore : IFraudInvestigationAuditStore
{
    private readonly ConcurrentQueue<FraudInvestigationAuditEvent> events = new();

    public Task AppendAsync(FraudInvestigationAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<FraudInvestigationAuditEvent>> GetByCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<FraudInvestigationAuditEvent> result = events.Where(x => x.CaseId == caseId).OrderBy(x => x.OccurredAtUtc).ToArray();
        return Task.FromResult(result);
    }
}