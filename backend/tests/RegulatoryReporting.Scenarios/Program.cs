using RegulatoryReporting.Application;
using RegulatoryReporting.Contracts;

var service = new RegulatoryReportService(
    new RegulatoryReportGenerator(),
    new RegulatoryReportValidator(),
    new RegulatorySubmissionService(),
    new RegulatoryExportService(),
    new RegulatoryReportAuditService(),
    new NoOpRegulatoryReportSigner());

var created = service.CreateReport(new CreateRegulatoryReportRequest
{
    ReportType = "ComplianceCaseReport",
    JurisdictionCode = "CI",
    AuthorityCode = "BCEAO",
    SourceCaseIds = new List<string> { "case-001", "case-002" },
    SubjectAwidIds = new List<string> { "aw-001", "aw-002" },
    PeriodStartUtc = DateTimeOffset.UtcNow.AddDays(-30),
    PeriodEndUtc = DateTimeOffset.UtcNow,
    CorrelationId = "corr-0011-6",
    CreatedBy = "reg.ops@afriwallet"
});
if (created.ReportId == Guid.Empty || created.Status != "DRAFT") throw new Exception("report creation failed");

if (created.EvidenceReferences.Count < 5 || string.IsNullOrWhiteSpace(created.CorrelationId)) throw new Exception("case data aggregation failed");

var generated = service.Generate(created.ReportId, new ReportActionRequest
{
    Actor = "reg.analyst@afriwallet",
    Role = "analyst",
    Reason = "Generate first report package"
});
if (generated.Status != "GENERATED" || generated.CurrentVersion != 1) throw new Exception("report generation failed");

var reviewed = service.Review(created.ReportId, new ReportActionRequest
{
    Actor = "reg.reviewer@afriwallet",
    Role = "analyst",
    Reason = "Move to review"
});
if (reviewed.Status != "UNDER_REVIEW") throw new Exception("review workflow failed");

var approved = service.Approve(created.ReportId, new ReportActionRequest
{
    Actor = "reg.manager@afriwallet",
    Role = "regulatory_officer",
    Reason = "Approved for authority"
});
if (approved.Status != "APPROVED") throw new Exception("approval workflow failed");

var submitted = service.Submit(created.ReportId, new ReportActionRequest
{
    Actor = "reg.manager@afriwallet",
    Role = "regulatory_officer",
    Reason = "Submit to authority"
});
if (submitted.Status != "SUBMITTED") throw new Exception("submission history failed");

var jsonExport = service.Export(created.ReportId, "json", "reg.manager@afriwallet");
if (jsonExport.Format != "json" || !jsonExport.Payload.Contains("ReportVersion")) throw new Exception("json export failed");

var csvExport = service.Export(created.ReportId, "csv", "reg.manager@afriwallet");
if (csvExport.Format != "csv" || !csvExport.Payload.Contains("report_reference")) throw new Exception("csv export failed");

var pdfExport = service.Export(created.ReportId, "pdf", "reg.manager@afriwallet");
if (pdfExport.Format != "pdf" || !pdfExport.Payload.Contains("AFRIWALLET REGULATORY REPORT")) throw new Exception("pdf export failed");

var rejected = service.Reject(created.ReportId, new ReportActionRequest
{
    Actor = "reg.manager@afriwallet",
    Role = "regulatory_officer",
    Reason = "Need additional details",
    ResponseCode = "RJ-01",
    ResponseMessage = "Missing narrative"
});
if (rejected.Status != "REJECTED") throw new Exception("submission history failed");

var regenerated = service.Generate(created.ReportId, new ReportActionRequest
{
    Actor = "reg.analyst@afriwallet",
    Role = "analyst",
    Reason = "Regenerate after rejection"
});
if (regenerated.CurrentVersion < 5) throw new Exception("versioning failed");

var submissions = service.GetSubmissions(created.ReportId);
if (submissions.Count < 1 || submissions[^1].Status != "REJECTED") throw new Exception("submission history failed");

var reportAfterRegen = service.GetReport(created.ReportId);
if (reportAfterRegen.Checksum != regenerated.Checksum || string.IsNullOrWhiteSpace(reportAfterRegen.Checksum)) throw new Exception("checksum verification failed");

var invalidTransitionRejected = false;
try
{
    service.Archive(created.ReportId, new ReportActionRequest
    {
        Actor = "reg.analyst@afriwallet",
        Role = "analyst",
        Reason = "Should fail"
    });
}
catch (InvalidOperationException)
{
    invalidTransitionRejected = true;
}

if (!invalidTransitionRejected) throw new Exception("invalid transition rejected failed");

if (reportAfterRegen.AuditEvents.Count < 8 || !reportAfterRegen.AuditEvents.Contains("REGULATORY_REPORT_VERSION_CREATED")) throw new Exception("audit generation failed");
if (reportAfterRegen.Telemetry.Metrics["afw_regulatory_report_export_total"] < 3) throw new Exception("telemetry generation failed");

Console.WriteLine("report creation ..................... PASS");
Console.WriteLine("case data aggregation ............... PASS");
Console.WriteLine("report generation ................... PASS");
Console.WriteLine("versioning .......................... PASS");
Console.WriteLine("review workflow ..................... PASS");
Console.WriteLine("approval workflow ................... PASS");
Console.WriteLine("submission history .................. PASS");
Console.WriteLine("json export ......................... PASS");
Console.WriteLine("csv export .......................... PASS");
Console.WriteLine("pdf export .......................... PASS");
Console.WriteLine("checksum verification ............... PASS");
Console.WriteLine("invalid transition rejected ......... PASS");
Console.WriteLine("audit generation .................... PASS");
Console.WriteLine("telemetry generation ................ PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0011.6 regulatory reporting scenarios passed.");
