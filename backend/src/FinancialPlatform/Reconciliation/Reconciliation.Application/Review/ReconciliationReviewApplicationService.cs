namespace Reconciliation.Application.Review;

public sealed class ReconciliationReviewApplicationService(
    IReconciliationReviewRepository repository,
    ReconciliationReviewQueueService reviewQueueService)
{
    public async Task<ReconciliationReviewQueue> EnqueueAsync(
        Matching.AutomaticCandidateClassificationBatch batch,
        DateTime queuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var queue = reviewQueueService.BuildQueue(batch, queuedAtUtc);
        await repository.AddQueueAsync(queue, cancellationToken);
        return queue;
    }

    public Task<ReconciliationReviewItem?> GetAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));
        return repository.GetAsync(reviewId, cancellationToken);
    }

    public Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(
        string partnerId,
        ReconciliationReviewStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partnerId))
            throw new ArgumentException("Partner id is required.", nameof(partnerId));
        return repository.ListAsync(partnerId.Trim(), status, cancellationToken);
    }

    public async Task<ReconciliationReviewItem?> DecideAsync(
        ReconciliationReviewDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var current = await repository.GetAsync(command.ReviewId, cancellationToken);
        if (current is null)
            return null;

        var decided = reviewQueueService.ApplyDecision(current, command);
        var replaced = await repository.TryReplaceAsync(decided, current.Status, cancellationToken);
        if (!replaced)
            throw new InvalidOperationException("Review item changed before the decision could be stored.");

        return decided;
    }
}
