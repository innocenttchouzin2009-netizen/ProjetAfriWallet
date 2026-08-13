using Support.Application;
using Support.Contracts;
using Support.Infrastructure;

var store = new InMemorySupportStore();
var service = new SupportCaseService(
    store,
    new AssignmentService(),
    new SlaService(),
    new EscalationService(),
    new SupportTimelineService(),
    new SupportNotificationService(),
    new SupportSearchService());

var created = service.CreateCase(new CreateSupportCaseRequest
{
    RequesterAwidId = "aw-1001",
    Category = "PAYMENT",
    Subject = "Transfer failed",
    Description = "Customer reported transfer timeout",
    Priority = "HIGH",
    Channel = "MOBILE_APP",
    RelatedTransactionId = "tx-001"
});

if (created.CaseReference.Length < 8 || created.Status != "ASSIGNED") throw new Exception("case creation failed");
if (created.AssignedTeam != "SUPPORT_PAYMENTS") throw new Exception("automatic assignment failed");

var reassigned = service.AssignCase(created.CaseId, new AssignCaseRequest
{
    AssignedTeam = "SUPPORT_L2",
    AssignedAgentId = "agent-77"
});
if (reassigned.AssignedTeam != "SUPPORT_L2" || reassigned.AssignedAgentId != "agent-77") throw new Exception("manual reassignment failed");

var customerMessage = service.AddMessage(created.CaseId, new AddSupportMessageRequest
{
    AuthorId = "aw-1001",
    IsFromCustomer = true,
    Body = "Any update on my payment?"
});
if (!customerMessage.IsFromCustomer) throw new Exception("customer message failed");

service.AddInternalNote(created.CaseId, new AddSupportNoteRequest
{
    AuthorAgentId = "agent-77",
    Content = "Internal fraud signal check"
});
var visibleMessages = service.GetCustomerVisibleMessages(created.CaseId);
if (visibleMessages.Count != 1 || visibleMessages[0].Body.Contains("Internal", StringComparison.OrdinalIgnoreCase)) throw new Exception("internal note visibility failed");

service.AddAttachment(created.CaseId, new AddSupportAttachmentRequest
{
    FileName = "evidence.pdf",
    ContentType = "application/pdf",
    SizeBytes = 120_000,
    UploadedBy = "aw-1001"
});
var attachmentValidationPassed = false;
try
{
    service.AddAttachment(created.CaseId, new AddSupportAttachmentRequest
    {
        FileName = "secret.exe",
        ContentType = "application/octet-stream",
        SizeBytes = 10,
        UploadedBy = "aw-1001"
    });
}
catch (InvalidOperationException)
{
    attachmentValidationPassed = true;
}
if (!attachmentValidationPassed) throw new Exception("attachment validation failed");

var slaCalculated = service.GetSla(created.CaseId);
if (slaCalculated.FirstResponseTargetMinutes != 120 || slaCalculated.ResolutionTargetMinutes != 1440) throw new Exception("sla calculation failed");

var warningCase = service.CreateCase(new CreateSupportCaseRequest
{
    RequesterAwidId = "aw-2002",
    Category = "CARD",
    Subject = "Card declined",
    Description = "Card payment rejected",
    Priority = "URGENT",
    Channel = "CHAT",
    OpenedAtUtcOverride = DateTimeOffset.UtcNow.AddHours(-3).AddMinutes(-50)
});
service.AddMessage(warningCase.CaseId, new AddSupportMessageRequest
{
    AuthorId = "agent-88",
    IsFromCustomer = false,
    Body = "We are investigating"
});
var warningSla = service.GetSla(warningCase.CaseId, DateTimeOffset.UtcNow);
if (!warningSla.WarningTriggered) throw new Exception("sla warning failed");

var breachCase = service.CreateCase(new CreateSupportCaseRequest
{
    RequesterAwidId = "aw-3003",
    Category = "BANKING",
    Subject = "Settlement missing",
    Description = "Expected settlement not received",
    Priority = "CRITICAL",
    Channel = "WEB_PORTAL",
    OpenedAtUtcOverride = DateTimeOffset.UtcNow.AddHours(-2)
});
var breachSla = service.GetSla(breachCase.CaseId, DateTimeOffset.UtcNow);
if (!breachSla.Breached || breachSla.Violations.Count == 0) throw new Exception("sla breach failed");

service.AddMessage(created.CaseId, new AddSupportMessageRequest
{
    AuthorId = "agent-77",
    IsFromCustomer = false,
    Body = "Investigation started"
});
var escalated = service.EscalateCase(created.CaseId, new EscalateCaseRequest
{
    Level = "L2",
    Reason = "Requires specialized payment rail analysis"
});
if (escalated.Status != "ESCALATED") throw new Exception("escalation flow failed");

var resolved = service.ResolveCase(created.CaseId, new ResolveCaseRequest
{
    ResolvedByAgentId = "agent-77",
    ResolutionSummary = "Partner timeout cleared and transfer confirmed"
});
if (resolved.Status != "RESOLVED") throw new Exception("resolution flow failed");

var closed = service.CloseCase(created.CaseId, new CloseCaseRequest
{
    ClosedByAgentId = "agent-77",
    Reason = "Customer confirmed resolution"
});
if (closed.Status != "CLOSED") throw new Exception("case closure failed");

var reopened = service.ReopenCase(created.CaseId, new ReopenCaseRequest
{
    ReopenedBy = "aw-1001",
    Reason = "Need further clarification"
});
if (reopened.Status != "IN_PROGRESS") throw new Exception("case reopening failed");

var timeline = service.GetTimeline(created.CaseId);
if (!timeline.Any(x => x.EventType == "CASE_CREATED") ||
    !timeline.Any(x => x.EventType == "CASE_ASSIGNED") ||
    !timeline.Any(x => x.EventType == "CASE_ESCALATED") ||
    !timeline.Any(x => x.EventType == "CASE_CLOSED") ||
    !timeline.Any(x => x.EventType == "CASE_REOPENED"))
{
    throw new Exception("timeline generation failed");
}

var notificationStats = service.GetNotificationStats();
if (notificationStats.ExternalNotificationsSent <= 0 || notificationStats.InternalAlertsSent <= 0)
{
    throw new Exception("notification integration failed");
}

var latest = service.GetCase(created.CaseId);
if (!latest.AuditEvents.Contains("SUPPORT_CASE_CREATED") ||
    !latest.AuditEvents.Contains("SUPPORT_CASE_ESCALATED") ||
    !latest.AuditEvents.Contains("SUPPORT_CASE_RESOLVED") ||
    !latest.AuditEvents.Contains("SUPPORT_CASE_CLOSED") ||
    !latest.AuditEvents.Contains("SUPPORT_CASE_REOPENED"))
{
    throw new Exception("audit generation failed");
}

if (!latest.Telemetry.ContainsKey("afw_support_cases_created_total") ||
    !latest.Telemetry.ContainsKey("afw_support_sla_breaches_total") ||
    latest.Telemetry.Keys.Any(x => x.Contains("awid", StringComparison.OrdinalIgnoreCase) || x.Contains("email", StringComparison.OrdinalIgnoreCase) || x.Contains("caseid", StringComparison.OrdinalIgnoreCase)))
{
    throw new Exception("telemetry generation failed");
}

Console.WriteLine("case creation ....................... PASS");
Console.WriteLine("automatic assignment ............... PASS");
Console.WriteLine("manual reassignment ................ PASS");
Console.WriteLine("customer message .................... PASS");
Console.WriteLine("internal note visibility ............ PASS");
Console.WriteLine("attachment validation ............... PASS");
Console.WriteLine("sla calculation ..................... PASS");
Console.WriteLine("sla warning ......................... PASS");
Console.WriteLine("sla breach .......................... PASS");
Console.WriteLine("escalation flow ..................... PASS");
Console.WriteLine("resolution flow ..................... PASS");
Console.WriteLine("case closure ........................ PASS");
Console.WriteLine("case reopening ...................... PASS");
Console.WriteLine("timeline generation ................. PASS");
Console.WriteLine("notification integration ............ PASS");
Console.WriteLine("audit generation .................... PASS");
Console.WriteLine("telemetry generation ................ PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0012.2 support case scenarios passed.");
