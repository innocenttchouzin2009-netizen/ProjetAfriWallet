using AfriWallet.Disputes.Eligibility.Domain.Classification;
using AfriWallet.Disputes.Eligibility.Domain.Eligibility;

namespace AfriWallet.Disputes.Eligibility.Application.Services;

public sealed record DisputeEligibilityResult(
    Guid DecisionId,
    Guid ClaimId,
    string Awid,
    DisputeEligibilityStatus Status,
    DisputeEligibilityReason PrimaryReason,
    DisputeClassification Classification,
    IReadOnlyCollection<EligibilityRuleEvaluation> Rules,
    DateTimeOffset EvaluatedAtUtc);
