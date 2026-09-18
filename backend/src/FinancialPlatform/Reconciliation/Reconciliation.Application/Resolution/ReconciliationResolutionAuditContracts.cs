using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Application.Resolution;

public sealed record ReconciliationResolutionAuditEntry(
    Guid AuditId,
    Guid ResolutionId,
    Guid ReviewId,
    ReconciliationResolutionDisposition Disposition,
    string ResolvedBy,
    string Rationale,
    string EvidenceReference,
    DateTime RecordedAtUtc);

public interface IReconciliationResolutionAuditReader
{
    Task<IReadOnlyList<ReconciliationResolutionAuditEntry>> ListByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);
}
