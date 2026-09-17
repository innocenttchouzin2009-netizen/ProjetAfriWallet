using Reconciliation.Application.Review.Resolution;

namespace Reconciliation.Api.Review;

public sealed record ReviewResolutionResponse(
    string Status,
    Guid ReviewId,
    string? PartnerId,
    string? EvidenceId,
    string? ResolvedBy,
    DateTime? ResolvedAtUtc,
    string? InternalRecordId,
    string? ExternalRecordId)
{
    public static ReviewResolutionResponse From(ReviewResolutionResult result) => new(
        result.Status.ToString(),
        result.ReviewId,
        result.Resolution?.PartnerId,
        result.Resolution?.EvidenceId,
        result.Resolution?.ResolvedBy,
        result.Resolution?.ResolvedAtUtc,
        result.Evidence?.InternalRecordId,
        result.Evidence?.ExternalRecordId);
}

public sealed record ReviewResolutionError(string Code, string Message);

public static class ReviewResolutionErrorCode
{
    public const string Validation = "RECONCILIATION_REVIEW_RESOLUTION_VALIDATION";
    public const string NotEligible = "RECONCILIATION_REVIEW_RESOLUTION_NOT_ELIGIBLE";
    public const string NoMatchingEvidence = "RECONCILIATION_REVIEW_RESOLUTION_NO_MATCHING_EVIDENCE";
    public const string Conflict = "RECONCILIATION_REVIEW_RESOLUTION_CONFLICT";
}
