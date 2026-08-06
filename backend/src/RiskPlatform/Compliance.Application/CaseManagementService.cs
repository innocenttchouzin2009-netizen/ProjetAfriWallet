using Compliance.Contracts;
using Compliance.Domain;

namespace Compliance.Application;

public sealed class CaseManagementService
{
    private readonly List<ComplianceCase> _cases = new();
    private readonly AssignmentService _assignmentService;
    private readonly InvestigationService _investigationService;
    private readonly EvidenceService _evidenceService;
    private readonly EscalationService _escalationService;

    public CaseManagementService(
        AssignmentService assignmentService,
        InvestigationService investigationService,
        EvidenceService evidenceService,
        EscalationService escalationService)
    {
        _assignmentService = assignmentService;
        _investigationService = investigationService;
        _evidenceService = evidenceService;
        _escalationService = escalationService;
    }

    public CaseResponse CreateCase(CreateCaseRequest request)
    {
        var entity = new ComplianceCase
        {
            Title = request.Title,
            Source = request.Source,
            Description = request.Description,
            Priority = request.Priority,
            Alerts = request.Alerts.Select(x => new ComplianceAlert
            {
                Source = x.Source,
                Type = x.Type,
                Details = x.Details,
                Severity = x.Severity,
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList()
        };

        entity.AuditEvents.Add("CASE_CREATED");

        var assignedInvestigator = string.IsNullOrWhiteSpace(request.AssignedInvestigator)
            ? _assignmentService.AutoAssignInvestigator(request.Source)
            : request.AssignedInvestigator;
        _assignmentService.Assign(entity, assignedInvestigator!, automatic: true);
        entity.AuditEvents.Add("CASE_AUTO_ASSIGNED");

        _investigationService.AddInvestigation(entity, _investigationService.CreateInvestigation("Initial triage", "OPEN"));
        entity.AuditEvents.Add("INVESTIGATION_STARTED");

        _cases.Add(entity);
        return Map(entity);
    }

    public IReadOnlyList<CaseResponse> ListCases() => _cases.Select(Map).ToList();

    public CaseResponse GetCase(Guid caseId)
    {
        var entity = GetCaseEntity(caseId);
        return Map(entity);
    }

    public CaseResponse UpdateCase(Guid caseId, UpdateCaseRequest request)
    {
        var entity = GetCaseEntity(caseId);
        if (!string.IsNullOrWhiteSpace(request.Title)) entity.Title = request.Title;
        if (!string.IsNullOrWhiteSpace(request.Description)) entity.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Status)) entity.Status = Enum.Parse<CaseStatus>(request.Status, true);
        if (!string.IsNullOrWhiteSpace(request.AssignedInvestigator)) entity.AssignedInvestigator = request.AssignedInvestigator!;
        if (!string.IsNullOrWhiteSpace(request.Priority)) entity.Priority = request.Priority;
        if (!string.IsNullOrWhiteSpace(request.InvestigatorNote))
        {
            _investigationService.AddNote(entity, request.NoteAuthor ?? "INVESTIGATOR", request.InvestigatorNote);
            entity.AuditEvents.Add("INVESTIGATION_NOTE_ADDED");
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.AuditEvents.Add("CASE_UPDATED");
        return Map(entity);
    }

    public CaseResponse AssignCase(Guid caseId, AssignCaseRequest request)
    {
        var entity = GetCaseEntity(caseId);
        _assignmentService.Assign(entity, request.Investigator, automatic: request.IsAutomatic);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.AuditEvents.Add(request.IsAutomatic ? "CASE_AUTO_ASSIGNED" : "CASE_MANUALLY_ASSIGNED");
        return Map(entity);
    }

    public CaseResponse AddEvidence(Guid caseId, EvidenceRequest request)
    {
        var entity = GetCaseEntity(caseId);
        _evidenceService.AddEvidence(entity, request.Label, request.Content);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.AuditEvents.Add("EVIDENCE_ADDED");
        return Map(entity);
    }

    public CaseResponse EscalateCase(Guid caseId, string reason, string escalatedBy)
    {
        var entity = GetCaseEntity(caseId);
        _escalationService.Escalate(entity, reason, escalatedBy);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.AuditEvents.Add("CASE_ESCALATED");
        return Map(entity);
    }

    public CaseResponse AddDecision(Guid caseId, DecisionRequest request)
    {
        var entity = GetCaseEntity(caseId);
        entity.Decisions.Add(new CaseDecision { DecisionType = request.DecisionType, Rationale = request.Rationale });
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.Status = CaseStatus.Resolved;
        entity.AuditEvents.Add("CASE_RESOLVED");
        if (request.CloseCase)
        {
            CloseCase(caseId, "SYSTEM");
        }

        return Map(entity);
    }

    public CaseResponse CloseCase(Guid caseId, string closedBy)
    {
        var entity = GetCaseEntity(caseId);
        entity.Status = CaseStatus.Closed;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _investigationService.AddNote(entity, closedBy, "Case closed");
        entity.AuditEvents.Add("CASE_CLOSED");
        return Map(entity);
    }

    private ComplianceCase GetCaseEntity(Guid caseId)
    {
        return _cases.Single(c => c.CaseId == caseId);
    }

    private static CaseResponse Map(ComplianceCase entity)
    {
        var ageMinutes = Math.Max(0d, (DateTimeOffset.UtcNow - entity.CreatedAt).TotalMinutes);
        var telemetryScore = Math.Max(10, entity.Alerts.Count * 20 + entity.Evidence.Count * 10 + entity.Decisions.Count * 15 + entity.Notes.Count * 5);

        return new CaseResponse
        {
            CaseId = entity.CaseId,
            Title = entity.Title,
            Source = entity.Source,
            Description = entity.Description,
            Status = entity.Status.ToString(),
            AssignedInvestigator = entity.AssignedInvestigator,
            Priority = entity.Priority,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Alerts = entity.Alerts.Select(x => new ComplianceAlertDto
            {
                AlertId = x.AlertId,
                Source = x.Source,
                Type = x.Type,
                Details = x.Details,
                Severity = x.Severity,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Investigations = entity.Investigations.Select(x => new InvestigationDto
            {
                InvestigationId = x.InvestigationId,
                Summary = x.Summary,
                Outcome = x.Outcome,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Evidence = entity.Evidence.Select(x => new EvidenceDto
            {
                EvidenceId = x.EvidenceId,
                Label = x.Label,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Decisions = entity.Decisions.Select(x => new CaseDecisionDto
            {
                DecisionId = x.DecisionId,
                DecisionType = x.DecisionType,
                Rationale = x.Rationale,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Notes = entity.Notes.Select(x => new InvestigatorNoteDto
            {
                NoteId = x.NoteId,
                Author = x.Author,
                Message = x.Message,
                CreatedAt = x.CreatedAt
            }).ToList(),
            AuditEvents = entity.AuditEvents.ToList(),
            Telemetry = new ComplianceTelemetry
            {
                CurrentStatus = entity.Status.ToString().ToUpperInvariant(),
                AlertCount = entity.Alerts.Count,
                EvidenceCount = entity.Evidence.Count,
                NoteCount = entity.Notes.Count,
                DecisionCount = entity.Decisions.Count,
                CaseAgeMinutes = ageMinutes,
                Score = telemetryScore
            }
        };
    }
}
