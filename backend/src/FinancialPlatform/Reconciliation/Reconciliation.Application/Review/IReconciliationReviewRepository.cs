namespace Reconciliation.Application.Review;

public interface IReconciliationReviewRepository
{
    Task AddQueueAsync(ReconciliationReviewQueue queue, CancellationToken cancellationToken = default);

    Task<ReconciliationReviewItem?> GetAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(
        string partnerId,
        ReconciliationReviewStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        ReconciliationReviewItem item,
        ReconciliationReviewStatus expectedStatus,
        CancellationToken cancellationToken = default);
}
