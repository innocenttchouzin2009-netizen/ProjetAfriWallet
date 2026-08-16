using AfriWallet.Compliance.CaseManagement.Application.Cases;
using AfriWallet.Compliance.CaseManagement.Application.Policies;
using AfriWallet.Compliance.CaseManagement.Domain.Cases;
using AfriWallet.Compliance.CaseManagement.Infrastructure;

static void Check(string name, bool ok) { Console.WriteLine($"{name,-42} {(ok ? "PASS" : "FAIL")}"); if (!ok) throw new InvalidOperationException($"Scenario failed: {name}"); }
var repository = new InMemoryComplianceCaseRepository();
var audit = new InMemoryComplianceCaseAuditStore();
var service = new ComplianceCaseService(repository, audit, new SystemComplianceCaseClock(), new CaseManagementPolicy());
var item = await service.CreateAsync(new("AWID-CASE-001", "Investigate AML signal", ComplianceCasePriority.Medium, "scenario-runner"));
Check("compliance case created", item.CaseId != Guid.Empty);
Check("case starts open", item.Status == ComplianceCaseStatus.Open);
item = await service.AddSourceAsync(new(item.CaseId, CaseSourceType.AmlMonitoring, "AML-ALERT-001", "High transaction risk alert", "scenario-runner"));
Check("AML source linked", item.SourceCount == 1 && item.Priority == ComplianceCasePriority.High);
item = await service.AssignAsync(new(item.CaseId, "analyst@example.test", "scenario-runner"));
Check("case assigned", item.Status == ComplianceCaseStatus.Assigned && item.Assignee is not null);
item = await service.StartReviewAsync(item.CaseId, "scenario-runner");
Check("case under review", item.Status == ComplianceCaseStatus.UnderReview);
item = await service.AddNoteAsync(new(item.CaseId, "Evidence reviewed.", "scenario-runner"));
Check("investigation note recorded", item.NoteCount == 1);
item = await service.EscalateAsync(new(item.CaseId, ComplianceCasePriority.Critical, "scenario-runner"));
Check("case escalated", item.Status == ComplianceCaseStatus.Escalated && item.Priority == ComplianceCasePriority.Critical);
item = await service.ResolveAsync(new(item.CaseId, ComplianceCaseDecision.FalsePositive, "scenario-runner"));
Check("false positive resolution", item.Status == ComplianceCaseStatus.Resolved && item.Decision == ComplianceCaseDecision.FalsePositive);
item = await service.CloseAsync(item.CaseId, "scenario-runner");
Check("case closed", item.Status == ComplianceCaseStatus.Closed);
var blocked = false;
try { await service.AddNoteAsync(new(item.CaseId, "Should not be allowed.", "scenario-runner")); } catch (InvalidOperationException) { blocked = true; }
Check("closed case immutable", blocked);
var events = await audit.GetByCaseAsync(item.CaseId);
Check("audit trail complete", events.Count >= 8);
var linked = await repository.GetAsync(item.CaseId);
Check("case persistence", linked is not null);
Console.WriteLine(); Console.WriteLine("All AFW-DLV-0016.6 compliance case management scenarios passed."); Console.WriteLine("Source engines duplicated: NO"); Console.WriteLine("Regulatory filing: NOT IMPLEMENTED"); Console.WriteLine("Legal determination: NOT CLAIMED"); Console.WriteLine("Decision: READY FOR REVIEW");