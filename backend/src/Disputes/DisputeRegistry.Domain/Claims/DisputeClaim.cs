using AfriWallet.Disputes.Registry.Domain.Evidence;
using AfriWallet.Disputes.Registry.Domain.History;

namespace AfriWallet.Disputes.Registry.Domain.Claims;

/// Canonical dispute claim record. It stores claims and their history only:
/// it never decides a refund or chargeback and never moves money.
public sealed class DisputeClaim
{
    private readonly List<DisputeEvidenceReference> evidence = new();
    private readonly List<DisputeClaimHistoryEntry> history = new();

    public DisputeClaim(
        Guid claimId,
        string awid,
        Guid transactionId,
        DisputeClaimType type,
        string reason,
        DisputeClaimAmount amount,
        string description,
        DisputeSourceChannel sourceChannel,
        DisputeClaimReferences references,
        DateTimeOffset createdAtUtc)
    {
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(claimId));
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.", nameof(awid));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Claim reason is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Claim description is required.", nameof(description));

        ClaimId = claimId;
        Awid = awid.Trim();
        TransactionId = transactionId;
        Type = type;
        Reason = reason.Trim();
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Description = description.Trim();
        SourceChannel = sourceChannel;
        References = references ?? throw new ArgumentNullException(nameof(references));
        Status = DisputeClaimStatus.Draft;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid ClaimId { get; }
    public string Awid { get; }
    public Guid TransactionId { get; }
    public DisputeClaimType Type { get; }
    public string Reason { get; }
    public DisputeClaimAmount Amount { get; }
    public string Description { get; }
    public DisputeSourceChannel SourceChannel { get; }
    public DisputeClaimReferences References { get; }
    public DisputeClaimStatus Status { get; private set; }
    public string? Outcome { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public IReadOnlyCollection<DisputeEvidenceReference> Evidence => evidence;
    public IReadOnlyCollection<DisputeClaimHistoryEntry> History => history;

    public void AddEvidenceReference(DisputeEvidenceReference reference, DateTimeOffset now)
    {
        EnsureMutable();
        evidence.Add(reference ?? throw new ArgumentNullException(nameof(reference)));
        UpdatedAtUtc = now;
    }

    public void Submit(string actor, DateTimeOffset now)
    {
        Transition(DisputeClaimStatus.Submitted, [DisputeClaimStatus.Draft], actor, "claim submitted", now);
        SubmittedAtUtc = now;
    }

    public void Open(string actor, DateTimeOffset now) =>
        Transition(DisputeClaimStatus.Open, [DisputeClaimStatus.Submitted], actor, "claim opened", now);

    public void StartReview(string actor, DateTimeOffset now) =>
        Transition(DisputeClaimStatus.UnderReview, [DisputeClaimStatus.Open], actor, "review started", now);

    public void Resolve(string outcome, string actor, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(outcome))
            throw new ArgumentException("Resolution outcome is required.", nameof(outcome));

        Transition(DisputeClaimStatus.Resolved, [DisputeClaimStatus.UnderReview], actor, outcome.Trim(), now);
        Outcome = outcome.Trim();
        ResolvedAtUtc = now;
    }

    public void Close(string actor, DateTimeOffset now)
    {
        Transition(DisputeClaimStatus.Closed, [DisputeClaimStatus.Resolved], actor, "claim closed", now);
        ClosedAtUtc = now;
    }

    public void Reject(string reason, string actor, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Transition(
            DisputeClaimStatus.Rejected,
            [DisputeClaimStatus.Submitted, DisputeClaimStatus.Open, DisputeClaimStatus.UnderReview],
            actor,
            reason.Trim(),
            now);
        Outcome = reason.Trim();
    }

    public void Cancel(string reason, string actor, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));

        Transition(
            DisputeClaimStatus.Cancelled,
            [DisputeClaimStatus.Draft, DisputeClaimStatus.Submitted, DisputeClaimStatus.Open],
            actor,
            reason.Trim(),
            now);
        Outcome = reason.Trim();
    }

    private void Transition(
        DisputeClaimStatus target,
        IReadOnlyCollection<DisputeClaimStatus> allowedSources,
        string actor,
        string reason,
        DateTimeOffset now)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));
        if (!allowedSources.Contains(Status))
            throw new InvalidOperationException($"Claim cannot move from {Status} to {target}.");

        history.Add(new DisputeClaimHistoryEntry(Guid.NewGuid(), Status, target, actor.Trim(), reason, now));
        Status = target;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable()
    {
        if (Status is DisputeClaimStatus.Closed or DisputeClaimStatus.Rejected or DisputeClaimStatus.Cancelled)
            throw new InvalidOperationException($"Claim in status {Status} is immutable.");
    }
}
