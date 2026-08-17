using AfriWallet.Fraud.Investigation.Application.Abstractions;
using AfriWallet.Fraud.Investigation.Application.Cases;
using AfriWallet.Fraud.Investigation.Application.Policies;
using AfriWallet.Fraud.Investigation.Domain.Cases;
using AfriWallet.Fraud.Investigation.Domain.Responses;
using AfriWallet.Fraud.Investigation.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-56} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
var decisions = new SandboxFraudDecisionEvidenceReader();
var repository = new InMemoryFraudCaseRepository();
var audit = new InMemoryFraudInvestigationAuditStore();
var transactionId = Guid.NewGuid();
decisions.Set(new FraudDecisionEvidenceSnapshot(Guid.NewGuid(), transactionId, "AWID-INVESTIGATION", 88, "Critical", "DeclineRecommended", now.AddMinutes(-5)));
var service = new FraudInvestigationService(repository, audit, decisions, new FixedClock(now), new FraudResponsePolicy());

var created = await service.CreateAsync(new CreateFraudCaseCommand("AWID-INVESTIGATION", transactionId, "Critical fraud decision review", FraudCasePriority.High, "scenario-runner"));
Check("fraud case created", created.CaseId != Guid.Empty, ref passed);
Check("fraud case starts open", created.Status == FraudCaseStatus.Open, ref passed);
Check("fraud decision evidence linked", created.EvidenceCount == 1, ref passed);
Check("initial response generated", created.ResponseCount == 1 && created.Priority == FraudCasePriority.High, ref passed);

var assigned = await service.AssignAsync(new AssignFraudCaseCommand(created.CaseId, "analyst-001", "scenario-runner"));
Check("fraud case assigned", assigned.Status == FraudCaseStatus.Assigned && assigned.AnalystId == "analyst-001", ref passed);
var investigating = await service.StartInvestigationAsync(created.CaseId, "analyst-001");
Check("investigation started", investigating.Status == FraudCaseStatus.UnderInvestigation, ref passed);
var noted = await service.AddNoteAsync(new AddFraudCaseNoteCommand(created.CaseId, "Reviewed decision evidence and transaction context.", "analyst-001"));
Check("investigation note recorded", noted.NoteCount == 1, ref passed);
var escalated = await service.EscalateAsync(new EscalateFraudCaseCommand(created.CaseId, FraudCasePriority.Critical, "analyst-001"));
Check("fraud case escalated", escalated.Status == FraudCaseStatus.Escalated && escalated.Priority == FraudCasePriority.Critical, ref passed);
var responded = await service.AddResponseAsync(new AddFraudResponseCommand(created.CaseId, FraudResponseType.AccountRestrictionRecommended, "Recommend controlled account review.", "analyst-001"));
Check("response recommendation recorded", responded.ResponseCount == 2, ref passed);
var resolved = await service.ResolveAsync(new ResolveFraudCaseCommand(created.CaseId, FraudCaseResolution.ConfirmedFraud, "analyst-001"));
Check("confirmed fraud resolution recorded", resolved.Status == FraudCaseStatus.Resolved && resolved.Resolution == FraudCaseResolution.ConfirmedFraud, ref passed);
var closed = await service.CloseAsync(created.CaseId, "analyst-001");
Check("fraud case closed", closed.Status == FraudCaseStatus.Closed, ref passed);

var immutable = false;
try { await service.AddNoteAsync(new AddFraudCaseNoteCommand(created.CaseId, "This must be rejected.", "analyst-001")); }
catch (InvalidOperationException) { immutable = true; }
Check("closed case immutable", immutable, ref passed);

var stored = await repository.GetAsync(created.CaseId);
Check("fraud case persisted", stored is not null, ref passed);
var events = await audit.GetByCaseAsync(created.CaseId);
Check("audit trail recorded", events.Count >= 7, ref passed);
Check("audit confirms no execution", events.All(x => x.Metadata["executionPerformed"] == "false"), ref passed);

var missingDecisionBlocked = false;
try { await service.CreateAsync(new CreateFraudCaseCommand("AWID-MISSING", Guid.NewGuid(), "Missing evidence test", FraudCasePriority.Low, "scenario-runner")); }
catch (InvalidOperationException) { missingDecisionBlocked = true; }
Check("case creation requires fraud decision", missingDecisionBlocked, ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0017.5 fraud investigation scenarios passed.");
Console.WriteLine("Payment mutation: NOT IMPLEMENTED");
Console.WriteLine("Account restriction execution: NOT IMPLEMENTED");
Console.WriteLine("Device revocation execution: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IFraudInvestigationClock
{
    public DateTimeOffset UtcNow { get; } = now;
}