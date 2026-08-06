using RegulatoryReporting.Contracts;
using RegulatoryReporting.Domain;

namespace RegulatoryReporting.Application;

public sealed class RegulatoryReportService
{
    private const string EventCreated = "REGULATORY_REPORT_CREATED";
    private const string EventGenerated = "REGULATORY_REPORT_GENERATED";
    private const string EventReviewed = "REGULATORY_REPORT_REVIEWED";
    private const string EventApproved = "REGULATORY_REPORT_APPROVED";
    private const string EventSubmitted = "REGULATORY_REPORT_SUBMITTED";
    private const string EventAccepted = "REGULATORY_REPORT_ACCEPTED";
    private const string EventRejected = "REGULATORY_REPORT_REJECTED";
    private const string EventArchived = "REGULATORY_REPORT_ARCHIVED";
    private const string EventExported = "REGULATORY_REPORT_EXPORTED";
    private const string EventVersionCreated = "REGULATORY_REPORT_VERSION_CREATED";

    private readonly List<RegulatoryReport> _reports = new();
    private readonly RegulatoryReportGenerator _generator;
    private readonly RegulatoryReportValidator _validator;
    private readonly RegulatorySubmissionService _submissionService;
    private readonly RegulatoryExportService _exportService;
    private readonly RegulatoryReportAuditService _auditService;
    private readonly IRegulatoryReportSigner _signer;

    private long _reportsCreatedTotal;
    private long _reportsSubmittedTotal;
    private long _reportsRejectedTotal;
    private long _reportExportTotal;
    private long _reportVersionsTotal;
    private double _lastGenerationDurationMs;

    public RegulatoryReportService(
        RegulatoryReportGenerator generator,
        RegulatoryReportValidator validator,
        RegulatorySubmissionService submissionService,
        RegulatoryExportService exportService,
        RegulatoryReportAuditService auditService,
        IRegulatoryReportSigner signer)
    {
        _generator = generator;
        _validator = validator;
        _submissionService = submissionService;
        _exportService = exportService;
        _auditService = auditService;
        _signer = signer;
    }

    public RegulatoryReportResponse CreateReport(CreateRegulatoryReportRequest request)
    {
        var report = _generator.BuildFromCreateRequest(request);
        _auditService.Record(report, EventCreated, request.CreatedBy);

        _reports.Add(report);
        _reportsCreatedTotal++;
        return Map(report);
    }

    public IReadOnlyList<RegulatoryReportResponse> ListReports() => _reports.Select(Map).ToList();

    public RegulatoryReportResponse GetReport(Guid reportId) => Map(GetReportEntity(reportId));

    public RegulatoryReportResponse Generate(Guid reportId, ReportActionRequest request)
    {
        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.Generated);

        var generated = _generator.Generate(report);
        report = generated.report;
        _lastGenerationDurationMs = generated.durationMs;
        report.Status = RegulatoryReportStatus.Generated;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        CreateVersion(report, request.Actor, string.IsNullOrWhiteSpace(request.Reason) ? "Initial generation" : request.Reason, "Generated report snapshot.");
        _auditService.Record(report, EventGenerated, request.Actor);
        return Map(report);
    }

    public RegulatoryReportResponse Review(Guid reportId, ReportActionRequest request)
    {
        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.UnderReview);
        report.Status = RegulatoryReportStatus.UnderReview;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        CreateVersion(report, request.Actor, EmptyToDefault(request.Reason, "Moved to review"), "Status changed to UNDER_REVIEW.");
        _auditService.Record(report, EventReviewed, request.Actor);
        return Map(report);
    }

    public RegulatoryReportResponse Approve(Guid reportId, ReportActionRequest request)
    {
        _validator.EnsurePrivilegedRole(request.Role, "approve");

        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.Approved);
        report.Status = RegulatoryReportStatus.Approved;
        report.ApprovedAtUtc = DateTimeOffset.UtcNow;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        CreateVersion(report, request.Actor, EmptyToDefault(request.Reason, "Approved for submission"), "Status changed to APPROVED.");
        _auditService.Record(report, EventApproved, request.Actor);
        return Map(report);
    }

    public RegulatoryReportResponse Submit(Guid reportId, ReportActionRequest request)
    {
        _validator.EnsurePrivilegedRole(request.Role, "submit");

        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.Submitted);
        report.Status = RegulatoryReportStatus.Submitted;
        report.SubmittedAtUtc = DateTimeOffset.UtcNow;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _submissionService.Submit(report, request.Actor);
        _reportsSubmittedTotal++;

        CreateVersion(report, request.Actor, EmptyToDefault(request.Reason, "Submitted to authority"), "Status changed to SUBMITTED.");
        _auditService.Record(report, EventSubmitted, request.Actor);
        return Map(report);
    }

    public RegulatoryReportResponse Accept(Guid reportId, ReportActionRequest request)
    {
        _validator.EnsurePrivilegedRole(request.Role, "accept");

        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.Accepted);
        report.Status = RegulatoryReportStatus.Accepted;
        report.AcceptedAtUtc = DateTimeOffset.UtcNow;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _submissionService.Accept(report, EmptyToDefault(request.ResponseCode, "ACCEPTED"), EmptyToDefault(request.ResponseMessage, "Accepted by authority"));

        CreateVersion(report, request.Actor, EmptyToDefault(request.Reason, "Accepted by authority"), "Status changed to ACCEPTED.");
        _auditService.Record(report, EventAccepted, request.Actor);
        return Map(report);
    }

    public RegulatoryReportResponse Reject(Guid reportId, ReportActionRequest request)
    {
        _validator.EnsurePrivilegedRole(request.Role, "reject");

        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.Rejected);
        report.Status = RegulatoryReportStatus.Rejected;
        report.RejectedAtUtc = DateTimeOffset.UtcNow;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _submissionService.Reject(report, EmptyToDefault(request.ResponseCode, "REJECTED"), EmptyToDefault(request.ResponseMessage, "Rejected by authority"));
        _reportsRejectedTotal++;

        CreateVersion(report, request.Actor, EmptyToDefault(request.Reason, "Rejected by authority"), "Status changed to REJECTED.");
        _auditService.Record(report, EventRejected, request.Actor);
        return Map(report);
    }

    public RegulatoryReportResponse Archive(Guid reportId, ReportActionRequest request)
    {
        var report = GetReportEntity(reportId);
        _validator.EnsureTransition(report.Status, RegulatoryReportStatus.Archived);
        report.Status = RegulatoryReportStatus.Archived;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        CreateVersion(report, request.Actor, EmptyToDefault(request.Reason, "Archived after acceptance"), "Status changed to ARCHIVED.");
        _auditService.Record(report, EventArchived, request.Actor);
        return Map(report);
    }

    public IReadOnlyList<RegulatoryReportVersionResponse> GetVersions(Guid reportId)
    {
        var report = GetReportEntity(reportId);
        return report.Versions.Select(x => new RegulatoryReportVersionResponse
        {
            VersionNumber = x.VersionNumber,
            Author = x.Author,
            CreatedAtUtc = x.CreatedAtUtc,
            ChangeReason = x.ChangeReason,
            Status = x.Status,
            DiffSummary = x.DiffSummary,
            SnapshotSummary = x.SnapshotSummary,
            Checksum = x.Checksum.Value
        }).ToList();
    }

    public ReportExportResponse Export(Guid reportId, string format, string actor)
    {
        var report = GetReportEntity(reportId);
        var exported = _exportService.Export(report, format);

        _reportExportTotal++;
        _auditService.Record(report, EventExported, actor);
        return exported;
    }

    public IReadOnlyList<RegulatorySubmissionResponse> GetSubmissions(Guid reportId)
    {
        var report = GetReportEntity(reportId);
        return report.Submissions.Select(x => new RegulatorySubmissionResponse
        {
            SubmissionId = x.SubmissionId,
            AuthorityCode = x.AuthorityCode,
            SubmittedBy = x.SubmittedBy,
            SubmittedAtUtc = x.SubmittedAtUtc,
            Status = x.Status,
            ResponseCode = x.ResponseCode,
            ResponseMessage = x.ResponseMessage,
            RespondedAtUtc = x.RespondedAtUtc
        }).ToList();
    }

    private RegulatoryReport GetReportEntity(Guid reportId)
    {
        return _reports.Single(x => x.ReportId == reportId);
    }

    private void CreateVersion(RegulatoryReport report, string author, string reason, string diff)
    {
        report.CurrentVersion++;
        report.Checksum = _generator.ComputeChecksum(report);
        report.Checksum.ReportVersion = report.CurrentVersion;
        report.Checksum.Signature = _signer.Sign(report.ReportReference, report.CurrentVersion, report.Checksum.GeneratedAtUtc, report.Checksum.Value);

        var version = new RegulatoryReportVersion
        {
            VersionNumber = report.CurrentVersion,
            Author = author,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ChangeReason = reason,
            Status = StatusToText(report.Status),
            DiffSummary = diff,
            SnapshotSummary = report.AggregationSummary,
            Checksum = report.Checksum
        };

        report.Versions.Add(version);
        _reportVersionsTotal++;
        _auditService.Record(report, EventVersionCreated, author);
    }

    private RegulatoryReportResponse Map(RegulatoryReport report)
    {
        return new RegulatoryReportResponse
        {
            ReportId = report.ReportId,
            ReportReference = report.ReportReference,
            ReportType = report.ReportType.ToString().ToUpperInvariant(),
            JurisdictionCode = report.JurisdictionCode,
            AuthorityCode = report.AuthorityCode,
            SourceCaseIds = report.SourceCaseIds.ToList(),
            SubjectAwidIds = report.SubjectAwidIds.ToList(),
            PeriodStartUtc = report.PeriodStartUtc,
            PeriodEndUtc = report.PeriodEndUtc,
            Status = StatusToText(report.Status),
            CurrentVersion = report.CurrentVersion,
            GeneratedAtUtc = report.GeneratedAtUtc,
            ApprovedAtUtc = report.ApprovedAtUtc,
            SubmittedAtUtc = report.SubmittedAtUtc,
            AcceptedAtUtc = report.AcceptedAtUtc,
            RejectedAtUtc = report.RejectedAtUtc,
            Checksum = report.Checksum?.Value,
            CorrelationId = report.CorrelationId,
            CreatedAtUtc = report.CreatedAtUtc,
            UpdatedAtUtc = report.UpdatedAtUtc,
            AggregationSummary = report.AggregationSummary,
            EvidenceReferences = report.EvidenceReferences.Select(x => new ReportEvidenceReferenceDto
            {
                SourceSystem = x.SourceSystem,
                SourceType = x.SourceType,
                SourceId = x.SourceId,
                Summary = x.Summary,
                ReferencedAtUtc = x.ReferencedAtUtc
            }).ToList(),
            Decisions = report.Decisions.ToList(),
            InvestigationNotes = report.InvestigationNotes.ToList(),
            AuditTimeline = report.AuditTimeline.ToList(),
            AuditEvents = report.AuditEvents.ToList(),
            Telemetry = new RegulatoryTelemetry
            {
                ReportsCreatedTotal = _reportsCreatedTotal,
                ReportsSubmittedTotal = _reportsSubmittedTotal,
                ReportsRejectedTotal = _reportsRejectedTotal,
                ReportExportTotal = _reportExportTotal,
                ReportVersionsTotal = _reportVersionsTotal,
                LastGenerationDurationMs = _lastGenerationDurationMs,
                Metrics = new Dictionary<string, double>
                {
                    ["afw_regulatory_reports_created_total"] = _reportsCreatedTotal,
                    ["afw_regulatory_reports_submitted_total"] = _reportsSubmittedTotal,
                    ["afw_regulatory_reports_rejected_total"] = _reportsRejectedTotal,
                    ["afw_regulatory_report_generation_duration_ms"] = _lastGenerationDurationMs,
                    ["afw_regulatory_report_export_total"] = _reportExportTotal,
                    ["afw_regulatory_report_versions_total"] = _reportVersionsTotal
                }
            }
        };
    }

    private static string EmptyToDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string StatusToText(RegulatoryReportStatus status)
    {
        return status switch
        {
            RegulatoryReportStatus.Draft => "DRAFT",
            RegulatoryReportStatus.Generated => "GENERATED",
            RegulatoryReportStatus.UnderReview => "UNDER_REVIEW",
            RegulatoryReportStatus.Approved => "APPROVED",
            RegulatoryReportStatus.Submitted => "SUBMITTED",
            RegulatoryReportStatus.Accepted => "ACCEPTED",
            RegulatoryReportStatus.Rejected => "REJECTED",
            RegulatoryReportStatus.Archived => "ARCHIVED",
            _ => status.ToString().ToUpperInvariant()
        };
    }
}
