using Reconciliation.Application.Interfaces;
using Reconciliation.Application.Review;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Domain.ReviewResolution;

namespace Reconciliation.Infrastructure.ReviewResolution;

public sealed class DurableReviewResolutionReviewReader(IReconciliationReviewRepository repository)
    : IReviewResolutionReviewReader
{
    public Task<ReconciliationReviewItem?> GetAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return repository.GetAsync(reviewId, cancellationToken);
    }
}

public sealed class ReconciliationDataReviewResolutionEvidenceMatcher(IReconciliationDataSource dataSource)
    : IReviewResolutionEvidenceMatcher
{
    public async Task<ReviewResolutionEvidence?> FindMatchingEvidenceAsync(
        ReconciliationReviewItem review,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(review.PartnerId))
            throw new ArgumentException("Review partner id is required.", nameof(review));
        if (review.QueuedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Review queue timestamp must be UTC.", nameof(review));
        if (string.IsNullOrWhiteSpace(review.InternalRecordId) && string.IsNullOrWhiteSpace(review.ExternalRecordId))
            return null;

        var fromUtc = DateTime.UnixEpoch;
        var toUtc = review.QueuedAtUtc;

        var internalRecord = string.IsNullOrWhiteSpace(review.InternalRecordId)
            ? null
            : (await dataSource.GetInternalRecordsAsync(review.PartnerId, fromUtc, toUtc, cancellationToken))
                .SingleOrDefault(x => string.Equals(x.RecordId, review.InternalRecordId, StringComparison.Ordinal));

        var externalRecord = string.IsNullOrWhiteSpace(review.ExternalRecordId)
            ? null
            : (await dataSource.GetExternalRecordsAsync(review.PartnerId, fromUtc, toUtc, cancellationToken))
                .SingleOrDefault(x => string.Equals(x.RecordId, review.ExternalRecordId, StringComparison.Ordinal));

        if ((!string.IsNullOrWhiteSpace(review.InternalRecordId) && internalRecord is null) ||
            (!string.IsNullOrWhiteSpace(review.ExternalRecordId) && externalRecord is null))
        {
            return null;
        }

        var observedAtUtc = new[]
            {
                internalRecord?.OccurredAtUtc,
                externalRecord?.OccurredAtUtc
            }
            .Where(x => x.HasValue)
            .Select(x => DateTime.SpecifyKind(x!.Value, DateTimeKind.Utc))
            .Max();

        return new ReviewResolutionEvidence(
            $"reconciliation:{review.ReviewId:N}",
            internalRecord?.RecordId,
            externalRecord?.RecordId,
            observedAtUtc).Validate();
    }
}
