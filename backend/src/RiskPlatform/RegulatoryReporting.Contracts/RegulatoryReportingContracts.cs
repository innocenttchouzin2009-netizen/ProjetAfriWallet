namespace RegulatoryReporting.Contracts;

public sealed class CreateRegulatoryReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public string JurisdictionCode { get; set; } = string.Empty;
    public string AuthorityCode { get; set; } = string.Empty;
    public List<string> SourceCaseIds { get; set; } = new();
    public List<string> SubjectAwidIds { get; set; } = new();
    public DateTimeOffset PeriodStartUtc { get; set; }
    public DateTimeOffset PeriodEndUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "system";
}

public sealed class ReportActionRequest
{
    public string Actor { get; set; } = "system";
    public string Role { get; set; } = "analyst";
    public string Reason { get; set; } = string.Empty;
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
}

public sealed class ReportExportResponse
{
    public string Format { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public int ReportVersion { get; init; }
    public string ReportReference { get; init; } = string.Empty;
}

public sealed class RegulatoryReportResponse
{
    public Guid ReportId { get; init; }
    public string ReportReference { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public string JurisdictionCode { get; init; } = string.Empty;
    public string AuthorityCode { get; init; } = string.Empty;
    public IReadOnlyList<string> SourceCaseIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SubjectAwidIds { get; init; } = Array.Empty<string>();
    public DateTimeOffset PeriodStartUtc { get; init; }
    public DateTimeOffset PeriodEndUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public int CurrentVersion { get; init; }
    public DateTimeOffset? GeneratedAtUtc { get; init; }
    public DateTimeOffset? ApprovedAtUtc { get; init; }
    public DateTimeOffset? SubmittedAtUtc { get; init; }
    public DateTimeOffset? AcceptedAtUtc { get; init; }
    public DateTimeOffset? RejectedAtUtc { get; init; }
    public string? Checksum { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public string AggregationSummary { get; init; } = string.Empty;
    public IReadOnlyList<ReportEvidenceReferenceDto> EvidenceReferences { get; init; } = Array.Empty<ReportEvidenceReferenceDto>();
    public IReadOnlyList<string> Decisions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InvestigationNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AuditTimeline { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AuditEvents { get; init; } = Array.Empty<string>();
    public RegulatoryTelemetry Telemetry { get; init; } = new();
}

public sealed class ReportEvidenceReferenceDto
{
    public string SourceSystem { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTimeOffset ReferencedAtUtc { get; init; }
}

public sealed class RegulatoryReportVersionResponse
{
    public int VersionNumber { get; init; }
    public string Author { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string DiffSummary { get; init; } = string.Empty;
    public string SnapshotSummary { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
}

public sealed class RegulatorySubmissionResponse
{
    public Guid SubmissionId { get; init; }
    public string AuthorityCode { get; init; } = string.Empty;
    public string SubmittedBy { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ResponseCode { get; init; }
    public string? ResponseMessage { get; init; }
    public DateTimeOffset? RespondedAtUtc { get; init; }
}

public sealed class RegulatoryTelemetry
{
    public long ReportsCreatedTotal { get; init; }
    public long ReportsSubmittedTotal { get; init; }
    public long ReportsRejectedTotal { get; init; }
    public long ReportExportTotal { get; init; }
    public long ReportVersionsTotal { get; init; }
    public double LastGenerationDurationMs { get; init; }
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
}
