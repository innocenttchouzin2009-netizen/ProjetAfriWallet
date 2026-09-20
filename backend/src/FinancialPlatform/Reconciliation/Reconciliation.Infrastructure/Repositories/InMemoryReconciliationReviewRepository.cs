using Reconciliation.Application.Review;

namespace Reconciliation.Infrastructure.Repositories;

public sealed class InMemoryReconciliationReviewRepository : IReconciliationReviewRepository
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, ReconciliationReviewItem> items = new();

    public Task AddQueueAsync(ReconciliationReviewQueue queue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            foreach (var item in queue.Items)
            {
                if (!items.TryAdd(item.ReviewId, item))
                    throw new InvalidOperationException($"Review item '{item.ReviewId}' already exists.");
            }
        }
        return Task.CompletedTask;
    }

    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            items.TryGetValue(reviewId, out var item);
            return Task.FromResult(item);
        }
    }

    public Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(
        string partnerId,
        ReconciliationReviewStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            var result = items.Values
                .Where(item => string.Equals(item.PartnerId, partnerId, StringComparison.Ordinal) &&
                               (status is null || item.Status == status))
                .OrderBy(item => item.QueuedAtUtc)
                .ThenBy(item => item.ReviewId)
                .ToArray();
            return Task.FromResult<IReadOnlyList<ReconciliationReviewItem>>(result);
        }
    }

    public Task<bool> TryReplaceAsync(
        ReconciliationReviewItem item,
        ReconciliationReviewStatus expectedStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (!items.TryGetValue(item.ReviewId, out var current) || current.Status != expectedStatus)
                return Task.FromResult(false);
            items[item.ReviewId] = item;
            return Task.FromResult(true);
        }
    }
}
