using Reconciliation.Domain.Remediation;

namespace Reconciliation.Application.Remediation;

public enum ReconciliationCorrectiveActionAuditEvent
{
    Created = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed record ReconciliationCorrectiveActionAuditEntry(
    Guid AuditId,
    Guid ActionId,
    Guid ResolutionId,
    ReconciliationCorrectiveActionAuditEvent Event,
    ReconciliationCorrectiveActionStatus Status,
    string ActorId,
    string? EvidenceReference,
    string? CancellationReason,
    DateTime RecordedAtUtc);

public interface IReconciliationCorrectiveActionAuditReader
{
    Task<IReadOnlyList<ReconciliationCorrectiveActionAuditEntry>> ListByResolutionIdAsync(
        Guid resolutionId,
        CancellationToken cancellationToken = default);
}
