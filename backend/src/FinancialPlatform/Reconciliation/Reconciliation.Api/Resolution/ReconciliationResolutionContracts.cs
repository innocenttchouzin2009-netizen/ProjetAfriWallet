using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Api.Resolution;

public sealed record CreateReconciliationResolutionRequest(
    string Disposition,
    string Rationale,
    string EvidenceReference);

public sealed record ReconciliationResolutionResponse(
    Guid ResolutionId,
    Guid ReviewId,
    string PartnerId,
    string? InternalRecordId,
    string? ExternalRecordId,
    string Disposition,
    string Rationale,
    string EvidenceReference,
    string ResolvedBy,
    DateTime ResolvedAtUtc)
{
    public static ReconciliationResolutionResponse From(ReconciliationResolution resolution) => new(
        resolution.ResolutionId,
        resolution.ReviewId,
        resolution.PartnerId,
        resolution.InternalRecordId,
        resolution.ExternalRecordId,
        resolution.Disposition.ToString(),
        resolution.Rationale,
        resolution.EvidenceReference,
        resolution.ResolvedBy,
        resolution.ResolvedAtUtc);
}

public sealed record ReconciliationResolutionAuditResponse(
    Guid AuditId,
    Guid ResolutionId,
    Guid ReviewId,
    string Disposition,
    string ResolvedBy,
    string Rationale,
    string EvidenceReference,
    DateTime RecordedAtUtc)
{
    public static ReconciliationResolutionAuditResponse From(ReconciliationResolutionAuditEntry entry) => new(
        entry.AuditId,
        entry.ResolutionId,
        entry.ReviewId,
        entry.Disposition.ToString(),
        entry.ResolvedBy,
        entry.Rationale,
        entry.EvidenceReference,
        entry.RecordedAtUtc);
}

public sealed record ReconciliationResolutionError(string Code, string Message);
