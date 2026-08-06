namespace RegulatoryReporting.Domain;

public sealed class RegulatoryReport
{
    public Guid ReportId { get; init; } = Guid.NewGuid();
    public string ReportReference { get; set; } = string.Empty;
    public RegulatoryReportType ReportType { get; set; }
    public string JurisdictionCode { get; set; } = string.Empty;
    public string AuthorityCode { get; set; } = string.Empty;
    public List<string> SourceCaseIds { get; set; } = new();
    public List<string> SubjectAwidIds { get; set; } = new();
    public DateTimeOffset PeriodStartUtc { get; set; }
    public DateTimeOffset PeriodEndUtc { get; set; }
    public RegulatoryReportStatus Status { get; set; } = RegulatoryReportStatus.Draft;
    public int CurrentVersion { get; set; }
    public DateTimeOffset? GeneratedAtUtc { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public ReportChecksum? Checksum { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string AggregationSummary { get; set; } = string.Empty;
    public List<ReportEvidenceReference> EvidenceReferences { get; set; } = new();
    public List<string> Decisions { get; set; } = new();
    public List<string> InvestigationNotes { get; set; } = new();
    public List<string> AuditTimeline { get; set; } = new();
    public List<RegulatoryReportVersion> Versions { get; set; } = new();
    public List<RegulatorySubmission> Submissions { get; set; } = new();
    public List<string> AuditEvents { get; set; } = new();
}
