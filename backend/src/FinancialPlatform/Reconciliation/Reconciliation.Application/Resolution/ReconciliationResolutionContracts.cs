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
    ReviewNotFinal = 4
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
}
