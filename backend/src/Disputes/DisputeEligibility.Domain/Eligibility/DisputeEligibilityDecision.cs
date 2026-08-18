using AfriWallet.Disputes.Eligibility.Domain.Classification;

namespace AfriWallet.Disputes.Eligibility.Domain.Eligibility;

public sealed class DisputeEligibilityDecision
{
    public DisputeEligibilityDecision(
        Guid decisionId,
        Guid claimId,
        string awid,
        DisputeEligibilityStatus status,
        DisputeEligibilityReason primaryReason,
        DisputeClassification classification,
        IReadOnlyCollection<EligibilityRuleEvaluation> rules,
        DateTimeOffset evaluatedAtUtc)
    {
        if (decisionId == Guid.Empty)
            throw new ArgumentException("Decision id is required.", nameof(decisionId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(claimId));
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.", nameof(awid));

        DecisionId = decisionId;
        ClaimId = claimId;
        Awid = awid.Trim();
        Status = status;
        PrimaryReason = primaryReason;
        Classification = classification ?? throw new ArgumentNullException(nameof(classification));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        EvaluatedAtUtc = evaluatedAtUtc;
    }

    public Guid DecisionId { get; }
    public Guid ClaimId { get; }
    public string Awid { get; }
    public DisputeEligibilityStatus Status { get; }
    public DisputeEligibilityReason PrimaryReason { get; }
    public DisputeClassification Classification { get; }
    public IReadOnlyCollection<EligibilityRuleEvaluation> Rules { get; }
    public DateTimeOffset EvaluatedAtUtc { get; }
}
