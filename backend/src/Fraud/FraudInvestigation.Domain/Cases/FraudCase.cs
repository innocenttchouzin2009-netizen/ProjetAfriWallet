using AfriWallet.Fraud.Investigation.Domain.Assignment;
using AfriWallet.Fraud.Investigation.Domain.Evidence;
using AfriWallet.Fraud.Investigation.Domain.Notes;
using AfriWallet.Fraud.Investigation.Domain.Responses;

namespace AfriWallet.Fraud.Investigation.Domain.Cases;

public sealed class FraudCase
{
    private readonly List<FraudEvidence> evidence = new();
    private readonly List<FraudInvestigationNote> notes = new();
    private readonly List<FraudResponseRecommendation> responses = new();

    public FraudCase(Guid caseId, string awid, Guid transactionId, string title, FraudCasePriority priority, DateTimeOffset createdAtUtc)
    {
        if (caseId == Guid.Empty) throw new ArgumentException("Case id is required.");
        if (transactionId == Guid.Empty) throw new ArgumentException("Transaction id is required.");
        if (string.IsNullOrWhiteSpace(awid)) throw new ArgumentException("AWID is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");

        CaseId = caseId;
        Awid = awid.Trim();
        TransactionId = transactionId;
        Title = title.Trim();
        Priority = priority;
        Status = FraudCaseStatus.Open;
        Resolution = FraudCaseResolution.None;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid CaseId { get; }
    public string Awid { get; }
    public Guid TransactionId { get; }
    public string Title { get; }
    public FraudCasePriority Priority { get; private set; }
    public FraudCaseStatus Status { get; private set; }
    public FraudCaseResolution Resolution { get; private set; }
    public FraudCaseAssignment? Assignment { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public IReadOnlyCollection<FraudEvidence> Evidence => evidence;
    public IReadOnlyCollection<FraudInvestigationNote> Notes => notes;
    public IReadOnlyCollection<FraudResponseRecommendation> Responses => responses;

    public void AddEvidence(FraudEvidence item, DateTimeOffset now) { EnsureMutable(); evidence.Add(item); UpdatedAtUtc = now; }

    public void Assign(string analystId, string assignedBy, DateTimeOffset now)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(analystId)) throw new ArgumentException("Analyst id is required.");
        Assignment = new FraudCaseAssignment(analystId.Trim(), assignedBy, now);
        Status = FraudCaseStatus.Assigned;
        UpdatedAtUtc = now;
    }

    public void StartInvestigation(DateTimeOffset now)
    {
        EnsureMutable();
        if (Assignment is null) throw new InvalidOperationException("Case must be assigned before investigation.");
        if (Status is not (FraudCaseStatus.Assigned or FraudCaseStatus.Open)) throw new InvalidOperationException("Case is not ready for investigation.");
        Status = FraudCaseStatus.UnderInvestigation;
        UpdatedAtUtc = now;
    }

    public void AddNote(FraudInvestigationNote note, DateTimeOffset now) { EnsureMutable(); notes.Add(note); UpdatedAtUtc = now; }

    public void Escalate(FraudCasePriority newPriority, DateTimeOffset now)
    {
        EnsureMutable();
        if (newPriority <= Priority) throw new InvalidOperationException("Escalation must increase priority.");
        Priority = newPriority;
        Status = FraudCaseStatus.Escalated;
        UpdatedAtUtc = now;
    }

    public void AddResponseRecommendation(FraudResponseRecommendation recommendation, DateTimeOffset now) { EnsureMutable(); responses.Add(recommendation); UpdatedAtUtc = now; }

    public void Resolve(FraudCaseResolution resolution, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (FraudCaseStatus.UnderInvestigation or FraudCaseStatus.Escalated)) throw new InvalidOperationException("Case must be investigated before resolution.");
        if (resolution == FraudCaseResolution.None) throw new InvalidOperationException("Resolution is required.");
        Resolution = resolution;
        Status = FraudCaseStatus.Resolved;
        ResolvedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != FraudCaseStatus.Resolved) throw new InvalidOperationException("Only resolved cases can be closed.");
        Status = FraudCaseStatus.Closed;
        ClosedAtUtc = now;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable()
    {
        if (Status == FraudCaseStatus.Closed) throw new InvalidOperationException("Closed fraud case is immutable.");
    }
}