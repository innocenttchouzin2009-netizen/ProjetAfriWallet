namespace Compliance.Contracts;

public sealed class CreateCaseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AssignedInvestigator { get; set; }
    public string Priority { get; set; } = "MEDIUM";
    public List<ComplianceAlertInput> Alerts { get; set; } = new();
}

public sealed class UpdateCaseRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? AssignedInvestigator { get; set; }
    public string? Priority { get; set; }
    public string? InvestigatorNote { get; set; }
    public string? NoteAuthor { get; set; }
}

public sealed class AssignCaseRequest
{
    public string Investigator { get; set; } = string.Empty;
    public bool IsAutomatic { get; set; }
}

public sealed class EvidenceRequest
{
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class DecisionRequest
{
    public string DecisionType { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public bool CloseCase { get; set; }
}

public sealed class CaseResponse
{
    public Guid CaseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string AssignedInvestigator { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyList<ComplianceAlertDto> Alerts { get; init; } = Array.Empty<ComplianceAlertDto>();
    public IReadOnlyList<InvestigationDto> Investigations { get; init; } = Array.Empty<InvestigationDto>();
    public IReadOnlyList<EvidenceDto> Evidence { get; init; } = Array.Empty<EvidenceDto>();
    public IReadOnlyList<CaseDecisionDto> Decisions { get; init; } = Array.Empty<CaseDecisionDto>();
    public IReadOnlyList<InvestigatorNoteDto> Notes { get; init; } = Array.Empty<InvestigatorNoteDto>();
    public IReadOnlyList<string> AuditEvents { get; init; } = Array.Empty<string>();
    public ComplianceTelemetry? Telemetry { get; init; }
}

public sealed class ComplianceAlertInput
{
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Severity { get; set; } = "MEDIUM";
}

public sealed class ComplianceAlertDto
{
    public Guid AlertId { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class InvestigationDto
{
    public Guid InvestigationId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class EvidenceDto
{
    public Guid EvidenceId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CaseDecisionDto
{
    public Guid DecisionId { get; init; }
    public string DecisionType { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class InvestigatorNoteDto
{
    public Guid NoteId { get; init; }
    public string Author { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ComplianceTelemetry
{
    public string CurrentStatus { get; init; } = string.Empty;
    public int AlertCount { get; init; }
    public int EvidenceCount { get; init; }
    public int NoteCount { get; init; }
    public int DecisionCount { get; init; }
    public double CaseAgeMinutes { get; init; }
    public int Score { get; init; }
}
