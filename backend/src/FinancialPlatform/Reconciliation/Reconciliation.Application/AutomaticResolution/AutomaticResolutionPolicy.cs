using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;

namespace Reconciliation.Application.AutomaticResolution;

public enum AutomaticResolutionDisposition
{
    AutoApprove = 1,
    ManualReview = 2,
    Escalate = 3
}

public sealed record AutomaticResolutionPolicyDecision(
    AutomaticResolutionDisposition Disposition,
    string Reason);

public sealed class AutomaticResolutionPolicy
{
    public AutomaticResolutionPolicyDecision Evaluate(ReconciliationReviewItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Status != ReconciliationReviewStatus.PendingReview)
        {
            return new AutomaticResolutionPolicyDecision(
                AutomaticResolutionDisposition.ManualReview,
                "Only pending review items are eligible for automatic resolution.");
        }

        if (item.MatchType == ReconciliationMatchType.Exact &&
            item.ConfidenceScore == 100 &&
            item.InternalRecordId is not null &&
            item.ExternalRecordId is not null &&
            item.AmountDifferenceMinor == 0)
        {
            return new AutomaticResolutionPolicyDecision(
                AutomaticResolutionDisposition.AutoApprove,
                "Exact reconciliation match with full confidence and zero amount difference.");
        }

        if (item.MatchType == ReconciliationMatchType.Unmatched)
        {
            return new AutomaticResolutionPolicyDecision(
                AutomaticResolutionDisposition.Escalate,
                "Unmatched reconciliation item requires operational investigation.");
        }

        return new AutomaticResolutionPolicyDecision(
            AutomaticResolutionDisposition.ManualReview,
            "Ambiguous reconciliation item remains in manual review.");
    }
}
