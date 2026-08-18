using AfriWallet.Disputes.Eligibility.Application.Abstractions;
using AfriWallet.Disputes.Eligibility.Application.Policies;
using AfriWallet.Disputes.Eligibility.Domain.Claims;
using AfriWallet.Disputes.Eligibility.Domain.Eligibility;

namespace AfriWallet.Disputes.Eligibility.Application.Services;

public sealed class DisputeEligibilityService(
    IDisputeClaimReader claims,
    ITransactionReferenceReader transactions,
    IDisputeEligibilityRepository repository,
    IDisputeEligibilityAuditStore audit,
    IDisputeEligibilityClock clock,
    DisputeEligibilityPolicy eligibilityPolicy,
    DisputeClassificationPolicy classificationPolicy)
{
    public async Task<DisputeEligibilityResult> EvaluateAsync(
        EvaluateDisputeEligibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClaimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var claim = await claims.GetAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException("Dispute claim snapshot was not found.");

        var classification = classificationPolicy.Classify(claim.ClaimType);
        var transaction = await transactions.GetAsync(claim.TransactionId, cancellationToken);

        IReadOnlyCollection<EligibilityRuleEvaluation> rules;
        DisputeEligibilityStatus status;
        DisputeEligibilityReason reason;

        if (transaction is null)
        {
            rules = [new EligibilityRuleEvaluation("DSP-ELG-000", false, "Referenced transaction must exist.")];
            status = DisputeEligibilityStatus.Ineligible;
            reason = DisputeEligibilityReason.TransactionNotFound;
        }
        else
        {
            rules = eligibilityPolicy.Evaluate(claim, transaction);
            status = ResolveStatus(claim.ClaimType, rules);
            reason = ResolveReason(claim, transaction, status);
        }

        var decision = new DisputeEligibilityDecision(
            Guid.NewGuid(),
            claim.ClaimId,
            claim.Awid,
            status,
            reason,
            classification,
            rules,
            clock.UtcNow);

        await repository.SaveAsync(decision, cancellationToken);
        await audit.AppendAsync(
            new DisputeEligibilityAuditEvent(
                Guid.NewGuid(),
                decision.DecisionId,
                decision.ClaimId,
                decision.Awid,
                decision.Status.ToString(),
                command.Actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["status"] = decision.Status.ToString(),
                    ["primaryReason"] = decision.PrimaryReason.ToString(),
                    ["category"] = decision.Classification.Category.ToString(),
                    ["refundDecisionPerformed"] = "false",
                    ["chargebackPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false"
                }),
            cancellationToken);

        return new DisputeEligibilityResult(
            decision.DecisionId,
            decision.ClaimId,
            decision.Awid,
            decision.Status,
            decision.PrimaryReason,
            decision.Classification,
            decision.Rules,
            decision.EvaluatedAtUtc);
    }

    private static DisputeEligibilityStatus ResolveStatus(
        DisputeClaimType claimType,
        IReadOnlyCollection<EligibilityRuleEvaluation> rules)
    {
        if (claimType == DisputeClaimType.Other)
            return DisputeEligibilityStatus.ManualReviewRequired;

        return rules.All(x => x.Passed)
            ? DisputeEligibilityStatus.Eligible
            : DisputeEligibilityStatus.Ineligible;
    }

    private static DisputeEligibilityReason ResolveReason(
        DisputeClaimSnapshot claim,
        TransactionReferenceSnapshot transaction,
        DisputeEligibilityStatus status)
    {
        if (status == DisputeEligibilityStatus.ManualReviewRequired)
            return DisputeEligibilityReason.ManualReviewRequired;
        if (status == DisputeEligibilityStatus.Eligible)
            return DisputeEligibilityReason.Eligible;

        if (!string.Equals(claim.Awid, transaction.Awid, StringComparison.OrdinalIgnoreCase))
            return DisputeEligibilityReason.AwidMismatch;
        if (!string.Equals(claim.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            return DisputeEligibilityReason.CurrencyMismatch;
        if (claim.ClaimAmountMinor <= 0 || claim.ClaimAmountMinor > transaction.AmountMinor)
            return DisputeEligibilityReason.ClaimAmountExceedsTransaction;

        var elapsed = claim.SubmittedAtUtc - transaction.OccurredAtUtc;
        if (elapsed < TimeSpan.Zero || elapsed > TimeSpan.FromDays(DisputeEligibilityPolicy.DefaultWindowDays))
            return DisputeEligibilityReason.SubmissionWindowExpired;

        if (!DisputeEligibilityPolicy.IsSettled(transaction.Status))
            return DisputeEligibilityReason.TransactionNotCompleted;

        return DisputeEligibilityReason.UnsupportedClaimType;
    }
}
