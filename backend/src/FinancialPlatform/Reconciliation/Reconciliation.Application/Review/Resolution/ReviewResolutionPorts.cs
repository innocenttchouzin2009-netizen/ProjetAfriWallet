using Reconciliation.Domain.ReviewResolution;

namespace Reconciliation.Application.Review.Resolution;

public interface IReviewResolutionReviewReader
{
    Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default);
}

public interface IReviewResolutionEvidenceMatcher
{
    Task<ReviewResolutionEvidence?> FindMatchingEvidenceAsync(
        ReconciliationReviewItem review,
        CancellationToken cancellationToken = default);
}

public interface IReviewResolutionStore
{
    Task<ReviewResolutionRecord?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<bool> TryAddAsync(ReviewResolutionRecord resolution, CancellationToken cancellationToken = default);
}
