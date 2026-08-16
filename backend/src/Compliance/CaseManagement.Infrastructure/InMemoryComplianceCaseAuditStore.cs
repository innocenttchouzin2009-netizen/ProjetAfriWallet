using System.Collections.Concurrent;
using AfriWallet.Compliance.CaseManagement.Application.Abstractions;

namespace AfriWallet.Compliance.CaseManagement.Infrastructure;

public sealed class InMemoryComplianceCaseAuditStore : IComplianceCaseAuditStore
{
    private readonly ConcurrentQueue<ComplianceCaseAuditEvent> _events = new();
    public Task AppendAsync(ComplianceCaseAuditEvent item, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); _events.Enqueue(item); return Task.CompletedTask; }
    public Task<IReadOnlyCollection<ComplianceCaseAuditEvent>> GetByCaseAsync(Guid caseId, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); IReadOnlyCollection<ComplianceCaseAuditEvent> result = _events.Where(x => x.CaseId == caseId).OrderBy(x => x.OccurredAtUtc).ToArray(); return Task.FromResult(result); }
}