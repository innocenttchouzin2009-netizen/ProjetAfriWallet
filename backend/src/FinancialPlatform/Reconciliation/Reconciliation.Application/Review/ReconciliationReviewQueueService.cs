using Reconciliation.Application.Matching;
using Reconciliation.Domain.Matches;

namespace Reconciliation.Application.Review;

public enum ReconciliationReviewStatus
{
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    Escalated = 4
}

public sealed record ReconciliationReviewItem(
    Guid ReviewId,
    string PartnerId,
    string? InternalRecordId,
    string? ExternalRecordId,
    ReconciliationMatchType MatchType,
    int ConfidenceScore,
    long? AmountDifferenceMinor,
    TimeSpan? TimeDifference,
    DateTime QueuedAtUtc,
    ReconciliationReviewStatus Status,
    string? ReviewerId,
    string? DecisionReason,
    DateTime? DecidedAtUtc);

public sealed record ReconciliationReviewQueue(
    string PartnerId,
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<ReconciliationReviewItem> Items);

public sealed record ReconciliationReviewDecisionCommand(
    Guid ReviewId,
    ReconciliationReviewStatus Decision,
    string ReviewerId,
    string? Reason,
    DateTime DecidedAtUtc);

public sealed class ReconciliationReviewQueueService
{
    public ReconciliationReviewQueue BuildQueue(
        AutomaticCandidateClassificationBatch batch,
        DateTime queuedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(batch);
        EnsureUtc(queuedAtUtc, nameof(queuedAtUtc));

        var items = batch.Candidates
            .Select(candidate => new ReconciliationReviewItem(
                Guid.NewGuid(),
                batch.PartnerId,
                candidate.InternalRecordId,
                candidate.ExternalRecordId,
                candidate.Type,
                candidate.ConfidenceScore,
                candidate.AmountDifferenceMinor,
                candidate.TimeDifference,
                queuedAtUtc,
                ReconciliationReviewStatus.PendingReview,
                null,
                null,
                null))
            .ToArray();

        return new ReconciliationReviewQueue(
            batch.PartnerId,
            batch.FromUtc,
            batch.ToUtc,
            items);
    }

    public ReconciliationReviewItem ApplyDecision(
        ReconciliationReviewItem item,
        ReconciliationReviewDecisionCommand command)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ReviewId == Guid.Empty || command.ReviewId != item.ReviewId)
        {
            throw new ArgumentException("Decision review id must match the review item.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.ReviewerId))
        {
            throw new ArgumentException("Reviewer id is required.", nameof(command));
        }

        EnsureUtc(command.DecidedAtUtc, nameof(command.DecidedAtUtc));
        if (command.DecidedAtUtc < item.QueuedAtUtc)
        {
            throw new ArgumentException("Decision time cannot be earlier than queue time.", nameof(command));
        }

        if (command.Decision == ReconciliationReviewStatus.PendingReview)
        {
            throw new ArgumentException("PendingReview is not a decision.", nameof(command));
        }

        if (item.Status is ReconciliationReviewStatus.Approved or ReconciliationReviewStatus.Rejected)
        {
            throw new InvalidOperationException("Approved or rejected review items are terminal.");
        }

        if (item.Status == ReconciliationReviewStatus.Escalated &&
            command.Decision == ReconciliationReviewStatus.Escalated)
        {
            throw new InvalidOperationException("An escalated review item cannot be escalated again.");
        }

        var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();
        if (command.Decision is ReconciliationReviewStatus.Rejected or ReconciliationReviewStatus.Escalated &&
            reason is null)
        {
            throw new ArgumentException("Reject and escalate decisions require a reason.", nameof(command));
        }

        return item with
        {
            Status = command.Decision,
            ReviewerId = command.ReviewerId.Trim(),
            DecisionReason = reason,
            DecidedAtUtc = command.DecidedAtUtc
        };
    }

    public IReadOnlyList<ReconciliationReviewItem> FilterByStatus(
        ReconciliationReviewQueue queue,
        ReconciliationReviewStatus status)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return queue.Items.Where(item => item.Status == status).ToArray();
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Review timestamps must be UTC.", parameterName);
        }
    }
}
