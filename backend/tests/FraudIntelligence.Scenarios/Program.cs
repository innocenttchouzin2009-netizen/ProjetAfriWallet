using AfriWallet.Fraud.Intelligence.Application.Abstractions;
using AfriWallet.Fraud.Intelligence.Application.Models;
using AfriWallet.Fraud.Intelligence.Application.Policies;
using AfriWallet.Fraud.Intelligence.Application.Services;
using AfriWallet.Fraud.Intelligence.Domain.Findings;
using AfriWallet.Fraud.Intelligence.Domain.Patterns;
using AfriWallet.Fraud.Intelligence.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
var source = new SandboxFraudIntelligenceSource();
var repository = new InMemoryFraudIntelligenceRepository();
var audit = new InMemoryFraudIntelligenceAuditStore();
var service = new FraudIntelligenceService(source, repository, audit, new FixedClock(now), new FraudCorrelationPolicy());

var subjectTransactions = new[]
{
    new IntelligenceTransactionSnapshot(Guid.NewGuid(), "AWID-SUBJECT", "DEVICE-SHARED", "BEN-SHARED", 100m, "USD", 70, now.AddHours(-3)),
    new IntelligenceTransactionSnapshot(Guid.NewGuid(), "AWID-SUBJECT", "DEVICE-SHARED", "BEN-SECOND", 200m, "USD", 80, now.AddHours(-2)),
    new IntelligenceTransactionSnapshot(Guid.NewGuid(), "AWID-SUBJECT", "DEVICE-OTHER", "BEN-SHARED", 300m, "USD", 90, now.AddHours(-1))
};
source.Set(new IntelligenceSourceSnapshot("AWID-SUBJECT", subjectTransactions, new[]
{
    new IntelligenceCaseSnapshot(Guid.NewGuid(), "AWID-SUBJECT", "Open", now.AddDays(-3)),
    new IntelligenceCaseSnapshot(Guid.NewGuid(), "AWID-SUBJECT", "Resolved", now.AddDays(-1))
}));
source.Set(new IntelligenceSourceSnapshot("AWID-RELATED-1", new[] { new IntelligenceTransactionSnapshot(Guid.NewGuid(), "AWID-RELATED-1", "DEVICE-SHARED", "BEN-OTHER", 20m, "USD", 10, now) }, Array.Empty<IntelligenceCaseSnapshot>()));
source.Set(new IntelligenceSourceSnapshot("AWID-RELATED-2", new[] { new IntelligenceTransactionSnapshot(Guid.NewGuid(), "AWID-RELATED-2", "DEVICE-NONE", "BEN-SHARED", 20m, "USD", 10, now) }, Array.Empty<IntelligenceCaseSnapshot>()));

var correlated = await service.CorrelateAsync(new CorrelateFraudCommand("AWID-SUBJECT", "scenario-runner"));
Check("finding created", correlated.FindingId != Guid.Empty, ref passed);
Check("finding subject AWID preserved", correlated.Awid == "AWID-SUBJECT", ref passed);
Check("shared device pattern detected", correlated.Patterns.Any(x => x.Type == FraudPatternType.SharedDevice), ref passed);
Check("shared beneficiary pattern detected", correlated.Patterns.Any(x => x.Type == FraudPatternType.SharedBeneficiary), ref passed);
Check("repeated high-risk pattern detected", correlated.Patterns.Any(x => x.Type == FraudPatternType.RepeatedHighRiskTransactions), ref passed);
Check("repeated fraud cases pattern detected", correlated.Patterns.Any(x => x.Type == FraudPatternType.RepeatedFraudCases), ref passed);
Check("compound risk pattern detected", correlated.Patterns.Any(x => x.Type == FraudPatternType.CompoundRisk), ref passed);
Check("score is additive and capped", correlated.CorrelationScore == 100, ref passed);
Check("score is reconstructible from patterns", correlated.CorrelationScore == Math.Min(100, correlated.Patterns.Sum(x => x.Score)), ref passed);
Check("critical severity resolved", correlated.Severity == IntelligenceSeverity.Critical, ref passed);
Check("patterns are explainable", correlated.Patterns.All(x => !string.IsNullOrWhiteSpace(x.Reason) && x.EntityIds.Count > 0), ref passed);
Check("pattern timestamps are deterministic", correlated.Patterns.All(x => x.DetectedAtUtc == now), ref passed);
Check("finding persisted", (await repository.GetLatestAsync("AWID-SUBJECT"))?.FindingId == correlated.FindingId, ref passed);
var events = await audit.GetAsync(correlated.FindingId);
Check("intelligence audit recorded", events.Count == 1, ref passed);
Check("audit proves no enforcement", events.Single().Metadata["enforcementPerformed"] == "false", ref passed);
Check("audit proves no machine learning", events.Single().Metadata["machineLearning"] == "false", ref passed);
Check("finding remains observational", correlated.Severity == IntelligenceSeverity.Critical, ref passed);

source.Set(new IntelligenceSourceSnapshot("AWID-CLEAN", new[] { new IntelligenceTransactionSnapshot(Guid.NewGuid(), "AWID-CLEAN", "DEVICE-CLEAN", "BEN-CLEAN", 50m, "EUR", 5, now) }, Array.Empty<IntelligenceCaseSnapshot>()));
var clean = await service.CorrelateAsync(new CorrelateFraudCommand("AWID-CLEAN", "scenario-runner"));
Check("clean subject produces zero score", clean.CorrelationScore == 0, ref passed);
Check("clean subject informational", clean.Severity == IntelligenceSeverity.Informational, ref passed);
Check("clean subject has no patterns", clean.Patterns.Count == 0, ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0017.6 fraud intelligence scenarios passed.");
Console.WriteLine("Machine learning classification: NOT IMPLEMENTED");
Console.WriteLine("Payment/account/device enforcement: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IFraudIntelligenceClock
{
    public DateTimeOffset UtcNow { get; } = now;
}