using Compliance.Application;
using Compliance.Contracts;

var service = new CaseManagementService(
    new AssignmentService(),
    new InvestigationService(),
    new EvidenceService(),
    new EscalationService());

var created = service.CreateCase(new CreateCaseRequest
{
    Title = "Fraud alert case",
    Source = "FRAUD",
    Description = "Potential account takeover",
    Priority = "HIGH",
    Alerts =
    {
        new ComplianceAlertInput
        {
            Source = "Fraud Detection Engine",
            Type = "FRAUD_ALERT",
            Details = "Velocity spike and unknown device",
            Severity = "HIGH"
        }
    }
});
if (created.CaseId == Guid.Empty || created.Status != "UnderReview") throw new Exception("case creation failed");

if (string.IsNullOrWhiteSpace(created.AssignedInvestigator)) throw new Exception("automatic assignment failed");

var manualAssignment = service.AssignCase(created.CaseId, new AssignCaseRequest
{
    Investigator = "investigator.manual@afriwallet",
    IsAutomatic = false
});
if (manualAssignment.AssignedInvestigator != "investigator.manual@afriwallet") throw new Exception("manual assignment failed");

var evidence = service.AddEvidence(created.CaseId, new EvidenceRequest
{
    Label = "Device telemetry",
    Content = "session_hash=afw-telemetry-001"
});
if (evidence.Evidence.Count != 1) throw new Exception("evidence attachment failed");

var withNote = service.UpdateCase(created.CaseId, new UpdateCaseRequest
{
    InvestigatorNote = "Customer confirms unusual login behavior.",
    NoteAuthor = "investigator.manual@afriwallet"
});
if (withNote.Notes.Count < 2) throw new Exception("investigation notes failed");

var escalated = service.EscalateCase(created.CaseId, "Cross-border pattern requires senior review", "risk.supervisor@afriwallet");
if (escalated.Status != "Escalated") throw new Exception("escalation flow failed");

var resolved = service.AddDecision(created.CaseId, new DecisionRequest
{
    DecisionType = "CLEAR_WITH_MONITORING",
    Rationale = "No financial loss observed; keep enhanced monitoring enabled.",
    CloseCase = false
});
if (resolved.Status != "Resolved" || resolved.Decisions.Count != 1) throw new Exception("resolution decision failed");

var closed = service.CloseCase(created.CaseId, "risk.supervisor@afriwallet");
if (closed.Status != "Closed") throw new Exception("case closure failed");

if (closed.AuditEvents.Count < 5) throw new Exception("audit generation failed");
if (closed.Telemetry == null || closed.Telemetry.Score <= 0) throw new Exception("telemetry generation failed");

Console.WriteLine("case creation ....................... PASS");
Console.WriteLine("automatic assignment ................ PASS");
Console.WriteLine("manual assignment ................... PASS");
Console.WriteLine("evidence attachment ................. PASS");
Console.WriteLine("investigation notes ................. PASS");
Console.WriteLine("escalation flow ..................... PASS");
Console.WriteLine("resolution decision ................. PASS");
Console.WriteLine("case closure ........................ PASS");
Console.WriteLine("audit generation .................... PASS");
Console.WriteLine("telemetry generation ................ PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0011.5 compliance scenarios passed.");
