using Reconciliation.Domain.ReviewResolution;

namespace Reconciliation.Application.Review.Resolution;

public sealed class ReviewResolutionApplicationService(
    IReviewResolutionReviewReader reviewReader,
    IReviewResolutionEvidenceMatcher evidenceMatcher,
    IReviewResolutionStore resolutionStore)
{
    public async Task<ReviewResolutionResult> ResolveAsync(
        ResolveReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.ReviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.ResolverId))
            throw new ArgumentException("Resolver id is required.", nameof(command));
        if (command.ResolvedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Resolution timestamp must be UTC.", nameof(command));

        var existing = await resolutionStore.GetAsync(command.ReviewId, cancellationToken);
        if (existing is not null)
            return ReviewResolutionResult.AlreadyResolved(existing);

        var review = await reviewReader.GetAsync(command.ReviewId, cancellationToken);
        if (review is null || review.Status != ReconciliationReviewStatus.Approved)
            return ReviewResolutionResult.NotEligible(command.ReviewId);

        if (command.ResolvedAtUtc < review.QueuedAtUtc)
            throw new ArgumentException("Resolution time cannot be earlier than queue time.", nameof(command));

        var evidence = await evidenceMatcher.FindMatchingEvidenceAsync(review, cancellationToken);
        if (evidence is null)
            return ReviewResolutionResult.NoMatchingEvidence(command.ReviewId);

        evidence = evidence.Validate();
        if (!EvidenceBelongsToReview(evidence, review))
            return ReviewResolutionResult.NoMatchingEvidence(command.ReviewId);

        var resolution = new ReviewResolutionRecord(
            review.ReviewId,
            review.PartnerId,
            evidence.EvidenceId,
            command.ResolverId,
            command.ResolvedAtUtc).Validate();

        if (!await resolutionStore.TryAddAsync(resolution, cancellationToken))
        {
            var concurrent = await resolutionStore.GetAsync(command.ReviewId, cancellationToken);
            if (concurrent is not null)
                return ReviewResolutionResult.AlreadyResolved(concurrent);
            throw new InvalidOperationException("Review resolution could not be stored.");
        }

        return ReviewResolutionResult.Resolved(resolution, evidence);
    }

    private static bool EvidenceBelongsToReview(
        ReviewResolutionEvidence evidence,
        ReconciliationReviewItem review)
    {
        var internalMatches = review.InternalRecordId is not null &&
            string.Equals(review.InternalRecordId, evidence.InternalRecordId, StringComparison.Ordinal);
        var externalMatches = review.ExternalRecordId is not null &&
            string.Equals(review.ExternalRecordId, evidence.ExternalRecordId, StringComparison.Ordinal);
        return internalMatches || externalMatches;
    }
}
