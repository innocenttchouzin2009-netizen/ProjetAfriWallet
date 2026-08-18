using AfriWallet.Disputes.Decision.Domain.Decisions;

namespace AfriWallet.Disputes.Decision.Application.Results;

public sealed record DisputeDecisionResult(
    Guid DecisionId,
    Guid ClaimId,
    Guid InvestigationId,
    string Awid,
    ResolutionDecisionType DecisionType,
    ResolutionDecisionStatus Status,
    ResolutionReasonCode ReasonCode,
    string PolicyVersion,
    bool RequiresManualApproval,
    int FactorCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
