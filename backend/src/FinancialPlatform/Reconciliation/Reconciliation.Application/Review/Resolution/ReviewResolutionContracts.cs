using Reconciliation.Domain.ReviewResolution;

namespace Reconciliation.Application.Review.Resolution;

public sealed record ResolveReviewCommand(
    Guid ReviewId,
    string ResolverId,
    DateTime ResolvedAtUtc);

public sealed record ReviewResolutionResult(
    ReviewResolutionStatus Status,
    Guid ReviewId,
    ReviewResolutionEvidence? Evidence,
    ReviewResolutionRecord? Resolution)
{
    public static ReviewResolutionResult Resolved(ReviewResolutionRecord resolution, ReviewResolutionEvidence evidence) =>
        new(ReviewResolutionStatus.Resolved, resolution.ReviewId, evidence, resolution);

    public static ReviewResolutionResult NoMatchingEvidence(Guid reviewId) =>
        new(ReviewResolutionStatus.NoMatchingEvidence, reviewId, null, null);

    public static ReviewResolutionResult AlreadyResolved(ReviewResolutionRecord resolution) =>
        new(ReviewResolutionStatus.AlreadyResolved, resolution.ReviewId, null, resolution);

    public static ReviewResolutionResult NotEligible(Guid reviewId) =>
        new(ReviewResolutionStatus.NotEligible, reviewId, null, null);
}
