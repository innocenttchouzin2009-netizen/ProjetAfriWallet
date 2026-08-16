using AfriWallet.Compliance.Screening.Application.Abstractions;
using AfriWallet.Compliance.Screening.Application.Matching;
using AfriWallet.Compliance.Screening.Application.Screening;
using AfriWallet.Compliance.Screening.Domain.Matching;
using AfriWallet.Compliance.Screening.Domain.Subjects;
using AfriWallet.Compliance.Screening.Infrastructure;
using AfriWallet.Compliance.Screening.Infrastructure.Providers;

static void Check(string name, bool condition)
{
    Console.WriteLine($"{name,-40} {(condition ? "PASS" : "FAIL")}");
    if (!condition)
        throw new InvalidOperationException($"Scenario failed: {name}");
}

var results = new InMemoryScreeningResultRepository();
var audit = new InMemoryScreeningAuditStore();
var clock = new SystemScreeningClock();
var registry = new ScreeningProviderRegistry(
    new IScreeningListProvider[]
    {
        new SandboxSanctionsProvider(),
        new SandboxPepProvider()
    });
var matcher = new ScreeningMatcher(ScreeningThresholds.Default);
var service = new ScreeningService(registry, results, audit, clock, matcher);

var blockedSubject = new ScreeningSubject(
    Guid.NewGuid(),
    ScreeningSubjectType.Individual,
    "Test Blocked Person",
    new DateOnly(1980, 1, 1),
    "CM",
    "AWID-SCREEN-001");
var blocked = await service.ScreenAsync(
    new ScreenSubjectCommand(blockedSubject, "scenario-runner"));
var sanctionsMatch = blocked.Matches.SingleOrDefault(
    match => match.SourceCode == "SANCTIONS-SBX");

Check("exact sanctions subject screened", blocked.SubjectId == blockedSubject.SubjectId);
Check("exact sanctions match blocks", blocked.FinalDecision == ScreeningDecision.Block);
Check("sanctions match score high", sanctionsMatch is not null && sanctionsMatch.Score >= 0.90);

var pepSubject = new ScreeningSubject(
    Guid.NewGuid(),
    ScreeningSubjectType.Individual,
    "Political Test Person",
    new DateOnly(1975, 5, 20),
    "FR",
    "AWID-SCREEN-002");
var pep = await service.ScreenAsync(
    new ScreenSubjectCommand(pepSubject, "scenario-runner"));

Check("PEP subject matched", pep.Matches.Any());
Check("PEP source present", pep.Matches.Any(match => match.SourceCode == "PEP-SBX"));

var clearSubject = new ScreeningSubject(
    Guid.NewGuid(),
    ScreeningSubjectType.Individual,
    "Completely Different Name",
    new DateOnly(1999, 9, 9),
    "JP",
    "AWID-SCREEN-003");
var clear = await service.ScreenAsync(
    new ScreenSubjectCommand(clearSubject, "scenario-runner"));

Check("unrelated subject clear", clear.FinalDecision == ScreeningDecision.Clear);

var auditEvents = await audit.GetBySubjectAsync(blockedSubject.SubjectId);
Check("audit event recorded", auditEvents.Count == 1);
Check("all sources sandbox", registry.All().All(provider => provider.Source.Sandbox));

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0016.3 sanctions and PEP screening scenarios passed.");
Console.WriteLine("Screening sources: SANDBOX ONLY");
Console.WriteLine("Regulatory screening certification: NOT CLAIMED");
Console.WriteLine("Decision: READY FOR REVIEW");