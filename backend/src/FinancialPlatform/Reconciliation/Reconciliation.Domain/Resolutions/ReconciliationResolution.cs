namespace Reconciliation.Domain.Resolutions;

public enum ReconciliationResolutionDisposition
{
    MatchConfirmed = 1,
    RecordCorrected = 2,
    VarianceAccepted = 3,
    NoActionRequired = 4
}

public sealed class ReconciliationResolution
{
    private ReconciliationResolution(
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        ReconciliationResolutionDisposition disposition,
        string rationale,
        string evidenceReference,
        string resolvedBy,
        DateTime resolvedAtUtc)
    {
        ResolutionId = resolutionId;
        ReviewId = reviewId;
        PartnerId = partnerId;
        InternalRecordId = internalRecordId;
        ExternalRecordId = externalRecordId;
        Disposition = disposition;
        Rationale = rationale;
        EvidenceReference = evidenceReference;
        ResolvedBy = resolvedBy;
        ResolvedAtUtc = resolvedAtUtc;
    }

    public Guid ResolutionId { get; }
    public Guid ReviewId { get; }
    public string PartnerId { get; }
    public string? InternalRecordId { get; }
    public string? ExternalRecordId { get; }
    public ReconciliationResolutionDisposition Disposition { get; }
    public string Rationale { get; }
    public string EvidenceReference { get; }
    public string ResolvedBy { get; }
    public DateTime ResolvedAtUtc { get; }

    public static ReconciliationResolution Create(
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        ReconciliationResolutionDisposition disposition,
        string rationale,
        string evidenceReference,
        string resolvedBy,
        DateTime resolvedAtUtc) =>
        CreateCore(
            Guid.NewGuid(),
            reviewId,
            partnerId,
            internalRecordId,
            externalRecordId,
            disposition,
            rationale,
            evidenceReference,
            resolvedBy,
            resolvedAtUtc);

    public static ReconciliationResolution Restore(
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        ReconciliationResolutionDisposition disposition,
        string rationale,
        string evidenceReference,
        string resolvedBy,
        DateTime resolvedAtUtc)
    {
        if (resolutionId == Guid.Empty)
            throw new ArgumentException("Resolution id is required.", nameof(resolutionId));

        return CreateCore(
            resolutionId,
            reviewId,
            partnerId,
            internalRecordId,
            externalRecordId,
            disposition,
            rationale,
            evidenceReference,
            resolvedBy,
            resolvedAtUtc);
    }

    private static ReconciliationResolution CreateCore(
        Guid resolutionId,
        Guid reviewId,
        string partnerId,
        string? internalRecordId,
        string? externalRecordId,
        ReconciliationResolutionDisposition disposition,
        string rationale,
        string evidenceReference,
        string resolvedBy,
        DateTime resolvedAtUtc)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));
        if (string.IsNullOrWhiteSpace(partnerId))
            throw new ArgumentException("Partner id is required.", nameof(partnerId));
        if (string.IsNullOrWhiteSpace(internalRecordId) && string.IsNullOrWhiteSpace(externalRecordId))
            throw new ArgumentException("At least one reconciliation record id is required.");
        if (!Enum.IsDefined(disposition))
            throw new ArgumentOutOfRangeException(nameof(disposition));
        if (string.IsNullOrWhiteSpace(rationale))
            throw new ArgumentException("Resolution rationale is required.", nameof(rationale));
        if (string.IsNullOrWhiteSpace(evidenceReference))
            throw new ArgumentException("Corrective evidence reference is required.", nameof(evidenceReference));
        if (string.IsNullOrWhiteSpace(resolvedBy))
            throw new ArgumentException("Resolver id is required.", nameof(resolvedBy));
        if (resolvedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Resolution timestamp must be UTC.", nameof(resolvedAtUtc));

        return new ReconciliationResolution(
            resolutionId,
            reviewId,
            partnerId.Trim(),
            Normalize(internalRecordId),
            Normalize(externalRecordId),
            disposition,
            rationale.Trim(),
            evidenceReference.Trim(),
            resolvedBy.Trim(),
            resolvedAtUtc);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
