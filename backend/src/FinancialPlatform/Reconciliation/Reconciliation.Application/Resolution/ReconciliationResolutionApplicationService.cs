using Reconciliation.Application.Review;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Application.Resolution;

public sealed class ReconciliationResolutionApplicationService(
    IReconciliationReviewRepository reviewRepository,
    IReconciliationResolutionRepository resolutionRepository)
{
    public async Task<ReconciliationResolutionExecutionResult> ResolveAsync(
        ResolveReconciliationReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCommand(command);

        var existing = await resolutionRepository.GetByReviewIdAsync(command.ReviewId, cancellationToken);
        if (existing is not null)
        {
            EnsureEquivalent(existing, command);
            return ReconciliationResolutionExecutionResult.Existing(existing);
        }

        var review = await reviewRepository.GetAsync(command.ReviewId, cancellationToken);
        if (review is null)
            return ReconciliationResolutionExecutionResult.ReviewNotFound();

        if (review.Status is not (ReconciliationReviewStatus.Approved or ReconciliationReviewStatus.Rejected))
            return ReconciliationResolutionExecutionResult.ReviewNotFinal();

        if (review.DecidedAtUtc is null)
            throw new InvalidOperationException("A final review must carry a decision timestamp.");

        if (command.ResolvedAtUtc < review.DecidedAtUtc.Value)
            throw new ArgumentException("Resolution time cannot be earlier than review decision time.", nameof(command));

        var resolution = ReconciliationResolution.Create(
            review.ReviewId,
            review.PartnerId,
            review.InternalRecordId,
            review.ExternalRecordId,
            command.Disposition,
            command.Rationale,
            command.EvidenceReference,
            command.ResolvedBy,
            command.ResolvedAtUtc);

        await resolutionRepository.AddAsync(resolution, cancellationToken);
        return ReconciliationResolutionExecutionResult.Created(resolution);
    }

    public Task<ReconciliationResolution?> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));
        return resolutionRepository.GetByReviewIdAsync(reviewId, cancellationToken);
    }

    private static void ValidateCommand(ResolveReconciliationReviewCommand command)
    {
        if (command.ReviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(command));
        if (!Enum.IsDefined(command.Disposition))
            throw new ArgumentOutOfRangeException(nameof(command));
        if (string.IsNullOrWhiteSpace(command.ResolvedBy))
            throw new ArgumentException("Resolver id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Rationale))
            throw new ArgumentException("Resolution rationale is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.EvidenceReference))
            throw new ArgumentException("Corrective evidence reference is required.", nameof(command));
        if (command.ResolvedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Resolution timestamp must be UTC.", nameof(command));
    }

    private static void EnsureEquivalent(
        ReconciliationResolution existing,
        ResolveReconciliationReviewCommand command)
    {
        if (existing.Disposition != command.Disposition ||
            !string.Equals(existing.ResolvedBy, command.ResolvedBy.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existing.Rationale, command.Rationale.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existing.EvidenceReference, command.EvidenceReference.Trim(), StringComparison.Ordinal) ||
            existing.ResolvedAtUtc != command.ResolvedAtUtc)
        {
            throw new InvalidOperationException("Review already has a different reconciliation resolution.");
        }
    }
}
