using Reconciliation.Application.Review;

namespace Reconciliation.Api.Review;

public sealed record ReconciliationReviewDecisionRequest(string Decision, string? Reason);

public sealed record ReconciliationReviewResponse(
    Guid ReviewId,
    string PartnerId,
    string? InternalRecordId,
    string? ExternalRecordId,
    string MatchType,
    int ConfidenceScore,
    long? AmountDifferenceMinor,
    double? TimeDifferenceSeconds,
    DateTime QueuedAtUtc,
    string Status,
    string? ReviewerId,
    string? DecisionReason,
    DateTime? DecidedAtUtc)
{
    public static ReconciliationReviewResponse From(ReconciliationReviewItem item) => new(
        item.ReviewId,
        item.PartnerId,
        item.InternalRecordId,
        item.ExternalRecordId,
        item.MatchType.ToString(),
        item.ConfidenceScore,
        item.AmountDifferenceMinor,
        item.TimeDifference?.TotalSeconds,
        item.QueuedAtUtc,
        item.Status.ToString(),
        item.ReviewerId,
        item.DecisionReason,
        item.DecidedAtUtc);
}

public sealed record ReconciliationReviewError(string Code, string Message);
