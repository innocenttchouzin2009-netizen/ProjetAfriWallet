namespace Reconciliation.Domain.ReviewResolution;

public enum ReviewResolutionStatus
{
    Resolved = 1,
    NoMatchingEvidence = 2,
    AlreadyResolved = 3,
    NotEligible = 4
}

public sealed record ReviewResolutionEvidence(
    string EvidenceId,
    string? InternalRecordId,
    string? ExternalRecordId,
    DateTime ObservedAtUtc)
{
    public ReviewResolutionEvidence Validate()
    {
        if (string.IsNullOrWhiteSpace(EvidenceId))
            throw new ArgumentException("Evidence id is required.", nameof(EvidenceId));
        if (ObservedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Evidence timestamp must be UTC.", nameof(ObservedAtUtc));
        if (string.IsNullOrWhiteSpace(InternalRecordId) && string.IsNullOrWhiteSpace(ExternalRecordId))
            throw new ArgumentException("Evidence must reference an internal or external record.");
        return this with
        {
            EvidenceId = EvidenceId.Trim(),
            InternalRecordId = string.IsNullOrWhiteSpace(InternalRecordId) ? null : InternalRecordId.Trim(),
            ExternalRecordId = string.IsNullOrWhiteSpace(ExternalRecordId) ? null : ExternalRecordId.Trim()
        };
    }
}

public sealed record ReviewResolutionRecord(
    Guid ReviewId,
    string PartnerId,
    string EvidenceId,
    string ResolvedBy,
    DateTime ResolvedAtUtc)
{
    public ReviewResolutionRecord Validate()
    {
        if (ReviewId == Guid.Empty) throw new ArgumentException("Review id is required.", nameof(ReviewId));
        if (string.IsNullOrWhiteSpace(PartnerId)) throw new ArgumentException("Partner id is required.", nameof(PartnerId));
        if (string.IsNullOrWhiteSpace(EvidenceId)) throw new ArgumentException("Evidence id is required.", nameof(EvidenceId));
        if (string.IsNullOrWhiteSpace(ResolvedBy)) throw new ArgumentException("Resolver id is required.", nameof(ResolvedBy));
        if (ResolvedAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Resolution timestamp must be UTC.", nameof(ResolvedAtUtc));
        return this with
        {
            PartnerId = PartnerId.Trim(),
            EvidenceId = EvidenceId.Trim(),
            ResolvedBy = ResolvedBy.Trim()
        };
    }
}
