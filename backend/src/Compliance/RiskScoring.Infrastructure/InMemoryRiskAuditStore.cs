using System.Collections.Concurrent;
using AfriWallet.Compliance.RiskScoring.Application.Abstractions;

namespace AfriWallet.Compliance.RiskScoring.Infrastructure;

public sealed class InMemoryRiskAuditStore : IRiskAuditStore
{
    private readonly ConcurrentQueue<RiskAuditEvent> _events = new();

    public Task AppendAsync(RiskAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RiskAuditEvent>> GetByAwidAsync(
        string awid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<RiskAuditEvent> result = _events
            .Where(item => string.Equals(item.Awid, awid, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.OccurredAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}