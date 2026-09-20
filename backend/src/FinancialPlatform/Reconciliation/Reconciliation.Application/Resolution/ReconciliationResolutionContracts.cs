using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Application.Resolution;

public sealed record ResolveReconciliationReviewCommand(
    Guid ReviewId,
    ReconciliationResolutionDisposition Disposition,
    string ResolvedBy,
    string Rationale,
    string EvidenceReference,
    DateTime ResolvedAtUtc);

public enum ReconciliationResolutionExecutionStatus
{
    Created = 1,
    Existing = 2,
    ReviewNotFound = 3,
    ReviewNotFinal = 4,
    AccessDenied = 5
}

public sealed record ReconciliationResolutionExecutionResult(
    ReconciliationResolutionExecutionStatus Status,
    ReconciliationResolution? Resolution)
{
    public static ReconciliationResolutionExecutionResult Created(ReconciliationResolution resolution) =>
        new(ReconciliationResolutionExecutionStatus.Created, resolution);

    public static ReconciliationResolutionExecutionResult Existing(ReconciliationResolution resolution) =>
        new(ReconciliationResolutionExecutionStatus.Existing, resolution);

    public static ReconciliationResolutionExecutionResult ReviewNotFound() =>
        new(ReconciliationResolutionExecutionStatus.ReviewNotFound, null);

    public static ReconciliationResolutionExecutionResult ReviewNotFinal() =>
        new(ReconciliationResolutionExecutionStatus.ReviewNotFinal, null);

    public static ReconciliationResolutionExecutionResult AccessDenied() =>
        new(ReconciliationResolutionExecutionStatus.AccessDenied, null);
}

public enum ReconciliationResolutionLookupStatus
{
    Found = 1,
    NotFound = 2,
    AccessDenied = 3
}

public sealed record ReconciliationResolutionLookupResult(
    ReconciliationResolutionLookupStatus Status,
    ReconciliationResolution? Resolution)
{
    public static ReconciliationResolutionLookupResult Found(ReconciliationResolution resolution) =>
        new(ReconciliationResolutionLookupStatus.Found, resolution);

    public static ReconciliationResolutionLookupResult NotFound() =>
        new(ReconciliationResolutionLookupStatus.NotFound, null);

    public static ReconciliationResolutionLookupResult AccessDenied() =>
        new(ReconciliationResolutionLookupStatus.AccessDenied, null);
}

public sealed record ReconciliationResolutionQuery(
    string? PartnerId = null,
    ReconciliationResolutionDisposition? Disposition = null,
    string? ResolvedBy = null,
    DateTime? ResolvedFromUtc = null,
    DateTime? ResolvedToUtc = null,
    int Limit = 100);

public sealed record ReconciliationResolutionRepositoryQuery(
    IReadOnlyCollection<string>? PartnerIds,
    ReconciliationResolutionDisposition? Disposition,
    string? ResolvedBy,
    DateTime? ResolvedFromUtc,
    DateTime? ResolvedToUtc,
    int Limit);

public enum ReconciliationResolutionListStatus
{
    Success = 1,
    AccessDenied = 2
}

public sealed record ReconciliationResolutionListResult(
    ReconciliationResolutionListStatus Status,
    IReadOnlyList<ReconciliationResolution> Resolutions)
{
    public static ReconciliationResolutionListResult Success(IReadOnlyList<ReconciliationResolution> resolutions) =>
        new(ReconciliationResolutionListStatus.Success, resolutions);

    public static ReconciliationResolutionListResult AccessDenied() =>
        new(ReconciliationResolutionListStatus.AccessDenied, Array.Empty<ReconciliationResolution>());
}
