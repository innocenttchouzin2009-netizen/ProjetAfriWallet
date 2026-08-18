using AfriWallet.Disputes.Decision.Application.Abstractions;
using AfriWallet.Disputes.Decision.Domain.Decisions;
using AfriWallet.Disputes.Decision.Domain.Policies;

namespace AfriWallet.Disputes.Decision.Application.Services;

public sealed record PolicyEvaluation(
    ResolutionDecisionType DecisionType,
    ResolutionReasonCode ReasonCode,
    bool RequiresManualApproval,
    DecisionPolicyVersion PolicyVersion,
    IReadOnlyCollection<DecisionFactor> Factors);

/// Deterministic, versioned policy: never executes a refund, chargeback, or ledger mutation.
public sealed class DisputeDecisionPolicy
{
    private const decimal ManualApprovalThreshold = 1000m;

    public PolicyEvaluation Evaluate(InvestigationOutcomeSnapshot investigation)
    {
        ArgumentNullException.ThrowIfNull(investigation);

        var factors = new List<DecisionFactor>
        {
            new("investigation.outcome", $"Investigation outcome is {investigation.Outcome}.", "DisputeInvestigation"),
            new("investigation.classification", $"Claim classification is {investigation.Classification}.", "DisputeEligibility"),
            new("investigation.disputedAmount", $"Disputed amount is {investigation.DisputedAmount} {investigation.Currency}.", "DisputeInvestigation")
        };

        if (string.Equals(investigation.Outcome, "EvidenceDoesNotSupportClaim", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.Decline,
                ResolutionReasonCode.EvidenceDoesNotSupportClaim,
                false,
                DecisionPolicyVersion.Current,
                factors);
        }

        if (string.Equals(investigation.Outcome, "InsufficientEvidence", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.ManualReview,
                ResolutionReasonCode.InsufficientEvidence,
                true,
                DecisionPolicyVersion.Current,
                factors);
        }

        if (string.Equals(investigation.Outcome, "ManualEscalationRequired", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.ManualReview,
                ResolutionReasonCode.InvestigationRequiresEscalation,
                true,
                DecisionPolicyVersion.Current,
                factors);
        }

        if (!string.Equals(investigation.Outcome, "EvidenceSupportsClaim", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.ManualReview,
                ResolutionReasonCode.PolicyRequiresManualReview,
                true,
                DecisionPolicyVersion.Current,
                factors);
        }

        var requiresApproval = investigation.DisputedAmount >= ManualApprovalThreshold;
        if (requiresApproval)
        {
            factors.Add(new DecisionFactor(
                "policy.manualApprovalThreshold",
                $"Disputed amount meets or exceeds the manual approval threshold of {ManualApprovalThreshold}.",
                "AFW-DISPUTE-RESOLUTION"));
        }

        if (string.Equals(investigation.Classification, "UnauthorizedTransaction", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.ChargebackRecommended,
                ResolutionReasonCode.UnauthorizedTransaction,
                requiresApproval,
                DecisionPolicyVersion.Current,
                factors);
        }

        if (string.Equals(investigation.Classification, "DuplicateTransaction", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.RefundRecommended,
                ResolutionReasonCode.DuplicateTransaction,
                requiresApproval,
                DecisionPolicyVersion.Current,
                factors);
        }

        if (string.Equals(investigation.Classification, "ProcessingError", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.RefundRecommended,
                ResolutionReasonCode.ProcessingError,
                requiresApproval,
                DecisionPolicyVersion.Current,
                factors);
        }

        if (string.Equals(investigation.Classification, "RefundNotProcessed", StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyEvaluation(
                ResolutionDecisionType.ChargebackRecommended,
                ResolutionReasonCode.RefundNotProcessed,
                requiresApproval,
                DecisionPolicyVersion.Current,
                factors);
        }

        return new PolicyEvaluation(
            ResolutionDecisionType.ManualReview,
            ResolutionReasonCode.UnsupportedClassification,
            true,
            DecisionPolicyVersion.Current,
            factors);
    }
}
