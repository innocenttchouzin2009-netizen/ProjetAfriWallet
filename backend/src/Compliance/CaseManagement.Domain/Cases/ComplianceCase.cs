using AfriWallet.Compliance.CaseManagement.Domain.Assignment;
using AfriWallet.Compliance.CaseManagement.Domain.Notes;
using AfriWallet.Compliance.CaseManagement.Domain.Sources;

namespace AfriWallet.Compliance.CaseManagement.Domain.Cases;

public sealed class ComplianceCase
{
    private readonly List<CaseSourceReference> _sources = [];
    private readonly List<ComplianceCaseNote> _notes = [];

    public ComplianceCase(
        Guid caseId,
        string awid,
        string title,
        ComplianceCasePriority priority,
        DateTimeOffset createdAtUtc)
    {
        if (caseId == Guid.Empty)
            throw new ArgumentException("Case ID is required.");
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        CaseId = caseId;
        Awid = awid.Trim();
        Title = title.Trim();
        Priority = priority;
        Status = ComplianceCaseStatus.Open;
        Decision = ComplianceCaseDecision.None;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid CaseId { get; }
    public string Awid { get; }
    public string Title { get; }
    public ComplianceCasePriority Priority { get; private set; }
    public ComplianceCaseStatus Status { get; private set; }
    public ComplianceCaseDecision Decision { get; private set; }
    public CaseAssignment? Assignment { get; private set; }
    public IReadOnlyCollection<CaseSourceReference> Sources => _sources.AsReadOnly();
    public IReadOnlyCollection<ComplianceCaseNote> Notes => _notes.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public void LinkSource(
        CaseSourceType type,
        string sourceId,
        string summary,
        ComplianceCasePriority recommendedPriority,
        DateTimeOffset now)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Source summary is required.", nameof(summary));
        if (_sources.Any(source => source.Type == type &&
            string.Equals(source.SourceId, sourceId.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Case source is already linked.");
        }

        _sources.Add(new CaseSourceReference(Guid.NewGuid(), type, sourceId.Trim(), summary.Trim(), now));
        if (recommendedPriority > Priority)
            Priority = recommendedPriority;
        UpdatedAtUtc = now;
    }

    public void Assign(string assignee, string assignedBy, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (ComplianceCaseStatus.Open or ComplianceCaseStatus.Assigned))
            throw new InvalidOperationException("Only open or assigned cases may be assigned.");
        if (string.IsNullOrWhiteSpace(assignee) || string.IsNullOrWhiteSpace(assignedBy))
            throw new ArgumentException("Assignee and assigning actor are required.");

        Assignment = new CaseAssignment(assignee.Trim(), assignedBy.Trim(), now);
        Status = ComplianceCaseStatus.Assigned;
        UpdatedAtUtc = now;
    }

    public void StartReview(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != ComplianceCaseStatus.Assigned)
            throw new InvalidOperationException("Only assigned cases can enter review.");
        Status = ComplianceCaseStatus.UnderReview;
        UpdatedAtUtc = now;
    }

    public void AddNote(string author, string content, DateTimeOffset now)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Note author and content are required.");
        _notes.Add(new ComplianceCaseNote(Guid.NewGuid(), author.Trim(), content.Trim(), now));
        UpdatedAtUtc = now;
    }

    public void Escalate(ComplianceCasePriority priority, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (ComplianceCaseStatus.UnderReview or ComplianceCaseStatus.Escalated))
            throw new InvalidOperationException("Only reviewed cases may be escalated.");
        if (priority <= Priority)
            throw new InvalidOperationException("Escalation must increase case priority.");
        Priority = priority;
        Status = ComplianceCaseStatus.Escalated;
        UpdatedAtUtc = now;
    }

    public void Resolve(ComplianceCaseDecision decision, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (ComplianceCaseStatus.UnderReview or ComplianceCaseStatus.Escalated))
            throw new InvalidOperationException("Only reviewed or escalated cases may be resolved.");
        if (decision == ComplianceCaseDecision.None)
            throw new InvalidOperationException("Resolution decision is required.");
        Decision = decision;
        Status = ComplianceCaseStatus.Resolved;
        ResolvedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != ComplianceCaseStatus.Resolved)
            throw new InvalidOperationException("Only resolved cases may be closed.");
        Status = ComplianceCaseStatus.Closed;
        ClosedAtUtc = now;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable()
    {
        if (Status == ComplianceCaseStatus.Closed)
            throw new InvalidOperationException("Closed compliance cases are immutable.");
    }
}