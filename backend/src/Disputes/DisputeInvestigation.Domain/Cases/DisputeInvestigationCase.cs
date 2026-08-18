using AfriWallet.Disputes.Investigation.Domain.Evidence;
using AfriWallet.Disputes.Investigation.Domain.Requests;
using AfriWallet.Disputes.Investigation.Domain.Timeline;

namespace AfriWallet.Disputes.Investigation.Domain.Cases;

/// Canonical investigation case: evidence, requests, and timeline only.
/// It never authorizes a refund, executes a chargeback, or moves money.
public sealed class DisputeInvestigationCase
{
    private readonly List<DisputeEvidence> evidence = new();
    private readonly List<EvidenceRequest> requests = new();
    private readonly List<InvestigationTimelineEntry> timeline = new();

    public DisputeInvestigationCase(Guid investigationId, Guid claimId, string awid, string actor, DateTimeOffset createdAtUtc)
    {
        if (investigationId == Guid.Empty)
            throw new ArgumentException("Investigation id is required.", nameof(investigationId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(claimId));
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.", nameof(awid));
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));

        InvestigationId = investigationId;
        ClaimId = claimId;
        Awid = awid.Trim();
        Status = InvestigationStatus.Open;
        Outcome = InvestigationOutcome.None;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;

        AppendTimeline("investigation.created", actor.Trim(), "Investigation created.", createdAtUtc);
    }

    public Guid InvestigationId { get; }
    public Guid ClaimId { get; }
    public string Awid { get; }
    public string? AnalystId { get; private set; }
    public InvestigationStatus Status { get; private set; }
    public InvestigationOutcome Outcome { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public IReadOnlyCollection<DisputeEvidence> Evidence => evidence;
    public IReadOnlyCollection<EvidenceRequest> Requests => requests;
    public IReadOnlyCollection<InvestigationTimelineEntry> Timeline => timeline;

    public void Assign(string analystId, string actor, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != InvestigationStatus.Open)
            throw new InvalidOperationException("Investigation must be open to assign an analyst.");
        if (string.IsNullOrWhiteSpace(analystId))
            throw new ArgumentException("Analyst id is required.", nameof(analystId));

        AnalystId = analystId.Trim();
        Status = InvestigationStatus.Assigned;
        UpdatedAtUtc = now;
        AppendTimeline("investigation.assigned", actor, $"Investigation assigned to {AnalystId}.", now);
    }

    public void RequestEvidence(EvidenceType type, string requestedFrom, string reason, string actor, DateTimeOffset now)
    {
        EnsureMutable();
        if (AnalystId is null)
            throw new InvalidOperationException("Investigation must be assigned before requesting evidence.");
        if (Status is not (InvestigationStatus.Assigned or InvestigationStatus.WaitingForEvidence))
            throw new InvalidOperationException($"Evidence cannot be requested while investigation is {Status}.");
        if (string.IsNullOrWhiteSpace(requestedFrom))
            throw new ArgumentException("Requested-from is required.", nameof(requestedFrom));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Request reason is required.", nameof(reason));

        requests.Add(new EvidenceRequest(Guid.NewGuid(), type, requestedFrom.Trim(), reason.Trim(), EvidenceRequestStatus.Open, now, null));
        Status = InvestigationStatus.WaitingForEvidence;
        UpdatedAtUtc = now;
        AppendTimeline("evidence.requested", actor, $"Evidence requested: {type} from {requestedFrom.Trim()}.", now);
    }

    public void AddEvidence(EvidenceType type, string reference, string description, EvidenceIntegrity integrity, string submittedBy, string actor, DateTimeOffset now)
    {
        EnsureMutable();
        if (AnalystId is null)
            throw new InvalidOperationException("Investigation must be assigned before evidence is added.");
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Evidence reference is required.", nameof(reference));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Evidence description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(submittedBy))
            throw new ArgumentException("Submitted-by is required.", nameof(submittedBy));
        ArgumentNullException.ThrowIfNull(integrity);

        if (evidence.Any(x => string.Equals(x.Integrity.Sha256, integrity.Sha256, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Duplicate evidence hash rejected.");

        evidence.Add(new DisputeEvidence(Guid.NewGuid(), type, reference.Trim(), description.Trim(), EvidenceStatus.Submitted, integrity, submittedBy.Trim(), now));
        UpdatedAtUtc = now;
        AppendTimeline("evidence.added", actor, $"Evidence added: {type} ({reference.Trim()}).", now);

        var matchIndex = requests.FindIndex(x => x.Status == EvidenceRequestStatus.Open && x.RequestedType == type);
        if (matchIndex >= 0)
            FulfillEvidenceRequest(requests[matchIndex].RequestId, actor, now);

        if (Status is InvestigationStatus.Assigned or InvestigationStatus.WaitingForEvidence &&
            !requests.Any(x => x.Status == EvidenceRequestStatus.Open))
        {
            Status = InvestigationStatus.UnderReview;
            UpdatedAtUtc = now;
            AppendTimeline("investigation.under_review", actor, "Investigation moved to review; all evidence requests resolved.", now);
        }
    }

    public void FulfillEvidenceRequest(Guid requestId, string actor, DateTimeOffset now)
    {
        EnsureMutable();
        var index = requests.FindIndex(x => x.RequestId == requestId);
        if (index < 0)
            throw new InvalidOperationException("Evidence request was not found.");

        var request = requests[index];
        if (request.Status != EvidenceRequestStatus.Open)
            throw new InvalidOperationException("Evidence request is not open.");

        requests[index] = request with { Status = EvidenceRequestStatus.Fulfilled, FulfilledAtUtc = now };
        UpdatedAtUtc = now;
        AppendTimeline("evidence.request.fulfilled", actor, $"Evidence request {requestId} fulfilled.", now);
    }

    public void Complete(InvestigationOutcome outcome, string actor, DateTimeOffset now)
    {
        EnsureMutable();
        if (outcome == InvestigationOutcome.None)
            throw new InvalidOperationException("Investigation outcome is required.");
        if (AnalystId is null)
            throw new InvalidOperationException("Investigation must be assigned.");
        if (requests.Any(x => x.Status == EvidenceRequestStatus.Open))
            throw new InvalidOperationException("Open evidence requests must be resolved first.");

        Outcome = outcome;
        Status = InvestigationStatus.Completed;
        CompletedAtUtc = now;
        UpdatedAtUtc = now;
        AppendTimeline("investigation.completed", actor, $"Investigation completed with outcome {outcome}.", now);
    }

    public void Close(string actor, DateTimeOffset now)
    {
        if (Status != InvestigationStatus.Completed)
            throw new InvalidOperationException("Only completed investigations can be closed.");

        Status = InvestigationStatus.Closed;
        ClosedAtUtc = now;
        UpdatedAtUtc = now;
        AppendTimeline("investigation.closed", actor, "Investigation closed.", now);
    }

    private void EnsureMutable()
    {
        if (Status is InvestigationStatus.Completed or InvestigationStatus.Closed)
            throw new InvalidOperationException("Terminal investigation state is immutable.");
    }

    private void AppendTimeline(string eventType, string actor, string description, DateTimeOffset now)
    {
        timeline.Add(new InvestigationTimelineEntry(Guid.NewGuid(), eventType, actor, description, now));
    }
}
