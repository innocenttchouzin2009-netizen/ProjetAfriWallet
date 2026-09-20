using Reconciliation.Application.Review;

namespace Reconciliation.Application.AutomaticResolution;

public sealed record AutomaticResolutionPlannedDecision(
    Guid ReviewId,
    AutomaticResolutionDisposition Disposition,
    ReconciliationReviewDecisionCommand? Command,
    string Reason);

public sealed record AutomaticResolutionDecisionBatch(
    DateTime DecidedAtUtc,
    IReadOnlyList<AutomaticResolutionPlannedDecision> Decisions);

public sealed class AutomaticResolutionDecisionOrchestrator(
    AutomaticResolutionPolicy policy)
{
    public AutomaticResolutionDecisionBatch Plan(
        IEnumerable<ReconciliationReviewItem> items,
        DateTime decidedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (decidedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Automatic resolution timestamp must be UTC.", nameof(decidedAtUtc));
        }

        var decisions = items.Select(item => Plan(item, decidedAtUtc)).ToArray();
        return new AutomaticResolutionDecisionBatch(decidedAtUtc, decisions);
    }

    public AutomaticResolutionPlannedDecision Plan(
        ReconciliationReviewItem item,
        DateTime decidedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (decidedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Automatic resolution timestamp must be UTC.", nameof(decidedAtUtc));
        }
        if (decidedAtUtc < item.QueuedAtUtc)
        {
            throw new ArgumentException("Automatic resolution cannot precede queue time.", nameof(decidedAtUtc));
        }

        var decision = policy.Evaluate(item);
        var command = decision.Disposition switch
        {
            AutomaticResolutionDisposition.AutoApprove => new ReconciliationReviewDecisionCommand(
                item.ReviewId,
                ReconciliationReviewStatus.Approved,
                "system:auto-resolution",
                decision.Reason,
                decidedAtUtc),
            AutomaticResolutionDisposition.Escalate => new ReconciliationReviewDecisionCommand(
                item.ReviewId,
                ReconciliationReviewStatus.Escalated,
                "system:auto-resolution",
                decision.Reason,
                decidedAtUtc),
            _ => null
        };

        return new AutomaticResolutionPlannedDecision(
            item.ReviewId,
            decision.Disposition,
            command,
            decision.Reason);
    }
}
