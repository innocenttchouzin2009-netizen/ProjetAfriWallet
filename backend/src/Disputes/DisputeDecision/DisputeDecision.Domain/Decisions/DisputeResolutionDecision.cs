using AfriWallet.Disputes.Decision.Domain.Policies;

namespace AfriWallet.Disputes.Decision.Domain.Decisions;

/// Recommends a dispute resolution; never executes a refund, chargeback, or ledger mutation.
public sealed class DisputeResolutionDecision
{
    private readonly List<DecisionFactor> _factors = new();

    public DisputeResolutionDecision(
        Guid decisionId,
        Guid claimId,
        Guid investigationId,
        string awid,
        ResolutionDecisionType decisionType,
        ResolutionReasonCode reasonCode,
        DecisionPolicyVersion policyVersion,
        bool requiresManualApproval,
        IEnumerable<DecisionFactor> factors,
        DateTimeOffset createdAtUtc)
    {
        if (decisionId == Guid.Empty)
            throw new ArgumentException("Decision id is required.", nameof(decisionId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(claimId));
        if (investigationId == Guid.Empty)
            throw new ArgumentException("Investigation id is required.", nameof(investigationId));
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.", nameof(awid));
        ArgumentNullException.ThrowIfNull(policyVersion);
        ArgumentNullException.ThrowIfNull(factors);

        DecisionId = decisionId;
        ClaimId = claimId;
        InvestigationId = investigationId;
        Awid = awid.Trim();
        DecisionType = decisionType;
        ReasonCode = reasonCode;
        PolicyVersion = policyVersion;
        _factors.AddRange(factors);

        RequiresManualApproval = requiresManualApproval;
        Status = requiresManualApproval
            ? ResolutionDecisionStatus.PendingManualApproval
            : decisionType == ResolutionDecisionType.Decline
                ? ResolutionDecisionStatus.Declined
                : ResolutionDecisionStatus.Approved;

        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid DecisionId { get; }
    public Guid ClaimId { get; }
    public Guid InvestigationId { get; }
    public string Awid { get; }
    public ResolutionDecisionType DecisionType { get; }
    public ResolutionReasonCode ReasonCode { get; }
    public DecisionPolicyVersion PolicyVersion { get; }
    public bool RequiresManualApproval { get; }
    public ResolutionDecisionStatus Status { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? ApprovalNote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public IReadOnlyCollection<DecisionFactor> Factors => _factors.AsReadOnly();

    public void Approve(string approver, string note, DateTimeOffset now)
    {
        if (!RequiresManualApproval)
            throw new InvalidOperationException("Decision does not require manual approval.");
        if (Status != ResolutionDecisionStatus.PendingManualApproval)
            throw new InvalidOperationException("Decision is not awaiting approval.");
        if (string.IsNullOrWhiteSpace(approver))
            throw new ArgumentException("Approver is required.");

        ApprovedBy = approver.Trim();
        ApprovalNote = note?.Trim();
        Status = DecisionType == ResolutionDecisionType.Decline
            ? ResolutionDecisionStatus.Declined
            : ResolutionDecisionStatus.Approved;
        ApprovedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Supersede(DateTimeOffset now)
    {
        if (Status == ResolutionDecisionStatus.Superseded)
            throw new InvalidOperationException("Decision is already superseded.");

        Status = ResolutionDecisionStatus.Superseded;
        UpdatedAtUtc = now;
    }
}
