using AfriWallet.Disputes.Intelligence.Application.Abstractions;
using AfriWallet.Disputes.Intelligence.Application.Models;
using AfriWallet.Disputes.Intelligence.Application.Policies;
using AfriWallet.Disputes.Intelligence.Application.Services;
using AfriWallet.Disputes.Intelligence.Domain.Findings;
using AfriWallet.Disputes.Intelligence.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";

var source = new SandboxDisputeIntelligenceSource();
var repository = new InMemoryDisputeIntelligenceRepository();
var audit = new InMemoryDisputeIntelligenceAuditStore();
var service = new CustomerProtectionService(source, repository, audit, new FixedClock(now), new CustomerProtectionPolicy());

ClaimSnapshot MakeClaim(string merchant, string beneficiary, DateTimeOffset submittedAt) =>
    new(Guid.NewGuid(), "AWID-DSP", merchant, beneficiary, "TransactionNotRecognized", submittedAt);

// --------------------------------------------------
// Clean subject: no patterns, no risk.
// --------------------------------------------------
var cleanClaim = MakeClaim("M-CLEAN", "B-CLEAN", now.AddDays(-1));
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-CLEAN",
    new[] { cleanClaim },
    new[] { new EligibilitySnapshot(cleanClaim.ClaimId, "Eligible", "UnauthorizedTransaction") },
    new[] { new InvestigationSnapshot(cleanClaim.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10)) },
    Array.Empty<DecisionSnapshot>(),
    Array.Empty<ResolutionSnapshot>()));

var clean = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-CLEAN", actor));
Check("clean subject score zero", clean.Score == 0, ref passed);
Check("clean subject informational", clean.Severity == ProtectionSeverity.Informational, ref passed);
Check("clean subject no action", clean.Recommendation == ProtectionRecommendation.NoAction, ref passed);
Check("clean subject has no patterns", clean.Patterns.Count == 0, ref passed);
Check("clean subject claim count metric", clean.Metrics.ClaimCount == 1, ref passed);

// --------------------------------------------------
// Repeated claims.
// --------------------------------------------------
var repeatClaims = new[]
{
    MakeClaim("M-R1", "B-R1", now.AddDays(-5)),
    MakeClaim("M-R2", "B-R2", now.AddDays(-4)),
    MakeClaim("M-R3", "B-R3", now.AddDays(-3))
};
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-REPEAT",
    repeatClaims,
    repeatClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    repeatClaims.Select(c => new InvestigationSnapshot(c.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))).ToArray(),
    Array.Empty<DecisionSnapshot>(),
    Array.Empty<ResolutionSnapshot>()));

var repeat = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-REPEAT", actor));
Check("repeated claims pattern present", repeat.Patterns.Any(x => x.Code == "DSP-INT-REPEATED-CLAIMS"), ref passed);
Check("repeated claims metrics claim count", repeat.Metrics.ClaimCount == 3, ref passed);

// --------------------------------------------------
// Merchant concentration.
// --------------------------------------------------
var merchantClaims = new[]
{
    MakeClaim("M-CONC", "B-A", now.AddDays(-2)),
    MakeClaim("M-CONC", "B-B", now.AddDays(-1))
};
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-MERCHANT",
    merchantClaims,
    merchantClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    merchantClaims.Select(c => new InvestigationSnapshot(c.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))).ToArray(),
    Array.Empty<DecisionSnapshot>(),
    Array.Empty<ResolutionSnapshot>()));

var merchant = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-MERCHANT", actor));
Check("merchant concentration pattern present", merchant.Patterns.Any(x => x.Code == "DSP-INT-MERCHANT-CONCENTRATION"), ref passed);
Check("merchant concentration repeated merchant metric", merchant.Metrics.RepeatedMerchantCount == 1, ref passed);
Check("merchant concentration score", merchant.Score == 20, ref passed);
Check("merchant concentration severity low", merchant.Severity == ProtectionSeverity.Low, ref passed);
Check("merchant concentration recommendation monitor", merchant.Recommendation == ProtectionRecommendation.Monitor, ref passed);

// --------------------------------------------------
// Beneficiary concentration.
// --------------------------------------------------
var beneficiaryClaims = new[]
{
    MakeClaim("M-C", "B-CONC", now.AddDays(-2)),
    MakeClaim("M-D", "B-CONC", now.AddDays(-1))
};
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-BENEFICIARY",
    beneficiaryClaims,
    beneficiaryClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    beneficiaryClaims.Select(c => new InvestigationSnapshot(c.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))).ToArray(),
    Array.Empty<DecisionSnapshot>(),
    Array.Empty<ResolutionSnapshot>()));

var beneficiary = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-BENEFICIARY", actor));
Check("beneficiary concentration pattern present", beneficiary.Patterns.Any(x => x.Code == "DSP-INT-BENEFICIARY-CONCENTRATION"), ref passed);
Check("beneficiary concentration recommendation monitor", beneficiary.Recommendation == ProtectionRecommendation.Monitor, ref passed);

// --------------------------------------------------
// Favorable decision concentration.
// --------------------------------------------------
var favorableClaims = new[] { MakeClaim("M-E", "B-E", now.AddDays(-2)), MakeClaim("M-F", "B-F", now.AddDays(-1)) };
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-FAVORABLE",
    favorableClaims,
    favorableClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    favorableClaims.Select(c => new InvestigationSnapshot(c.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))).ToArray(),
    new[]
    {
        new DecisionSnapshot(favorableClaims[0].ClaimId, "RefundRecommended", "Approved"),
        new DecisionSnapshot(favorableClaims[1].ClaimId, "ChargebackRecommended", "Approved")
    },
    Array.Empty<ResolutionSnapshot>()));

var favorable = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-FAVORABLE", actor));
Check("favorable decision pattern present", favorable.Patterns.Any(x => x.Code == "DSP-INT-FAVORABLE-DECISION-CONCENTRATION"), ref passed);
Check("favorable decision metrics count", favorable.Metrics.FavorableDecisionCount == 2, ref passed);

// --------------------------------------------------
// Resolution failures force customer protection review despite a low score.
// --------------------------------------------------
var failureClaims = new[] { MakeClaim("M-G", "B-G", now.AddDays(-2)), MakeClaim("M-H", "B-H", now.AddDays(-1)) };
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-FAILURES",
    failureClaims,
    failureClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    failureClaims.Select(c => new InvestigationSnapshot(c.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))).ToArray(),
    Array.Empty<DecisionSnapshot>(),
    new[]
    {
        new ResolutionSnapshot(failureClaims[0].ClaimId, "Refund", "Failed", 3),
        new ResolutionSnapshot(failureClaims[1].ClaimId, "Chargeback", "ManualInterventionRequired", 3)
    }));

var failures = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-FAILURES", actor));
Check("resolution failures pattern present", failures.Patterns.Any(x => x.Code == "DSP-INT-RESOLUTION-FAILURES"), ref passed);
Check("resolution failures forces protection review", failures.Recommendation == ProtectionRecommendation.CustomerProtectionReview, ref passed);
Check("resolution failures score below thirty", failures.Score < 30, ref passed);

// --------------------------------------------------
// Slow resolution.
// --------------------------------------------------
var slowClaim = MakeClaim("M-SLOW", "B-SLOW", now.AddDays(-10));
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-SLOW",
    new[] { slowClaim },
    new[] { new EligibilitySnapshot(slowClaim.ClaimId, "Eligible", "UnauthorizedTransaction") },
    new[] { new InvestigationSnapshot(slowClaim.ClaimId, "EvidenceSupportsClaim", now.AddDays(-9), now.AddDays(-5)) },
    Array.Empty<DecisionSnapshot>(),
    Array.Empty<ResolutionSnapshot>()));

var slow = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-SLOW", actor));
Check("slow resolution pattern present", slow.Patterns.Any(x => x.Code == "DSP-INT-SLOW-RESOLUTION"), ref passed);
Check("slow resolution average hours metric", slow.Metrics.AverageResolutionHours > 72, ref passed);

// --------------------------------------------------
// Compound risk: three independent patterns trigger a fourth.
// --------------------------------------------------
var compoundClaims = new[]
{
    MakeClaim("M-COMP", "B-1", now.AddDays(-3)),
    MakeClaim("M-COMP", "B-2", now.AddDays(-2)),
    MakeClaim("M-OTHER", "B-3", now.AddDays(-1))
};
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-COMPOUND",
    compoundClaims,
    compoundClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    compoundClaims.Select(c => new InvestigationSnapshot(c.ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))).ToArray(),
    Array.Empty<DecisionSnapshot>(),
    new[]
    {
        new ResolutionSnapshot(compoundClaims[0].ClaimId, "Refund", "Failed", 3),
        new ResolutionSnapshot(compoundClaims[1].ClaimId, "Chargeback", "ManualInterventionRequired", 3)
    }));

var compound = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-COMPOUND", actor));
Check("compound risk pattern present", compound.Patterns.Any(x => x.Code == "DSP-INT-COMPOUND-RISK"), ref passed);
Check("compound risk pattern count", compound.Patterns.Count == 4, ref passed);
Check("compound risk score", compound.Score == 75, ref passed);
Check("compound risk severity high", compound.Severity == ProtectionSeverity.High, ref passed);
Check("compound risk recommendation review merchant", compound.Recommendation == ProtectionRecommendation.ReviewMerchant, ref passed);

// --------------------------------------------------
// Critical subject: score clamps at 100 and escalates operations.
// --------------------------------------------------
var criticalClaims = new[]
{
    MakeClaim("M-CRIT", "B-C1", now.AddDays(-5)),
    MakeClaim("M-CRIT", "B-C2", now.AddDays(-4)),
    MakeClaim("M-CRIT", "B-C3", now.AddDays(-3)),
    MakeClaim("M-OTHER2", "B-C4", now.AddDays(-2)),
    MakeClaim("M-OTHER3", "B-C5", now.AddDays(-1))
};
source.Set(new DisputeIntelligenceSnapshot(
    "AWID-CRITICAL",
    criticalClaims,
    criticalClaims.Select(c => new EligibilitySnapshot(c.ClaimId, "Eligible", "UnauthorizedTransaction")).ToArray(),
    new[]
    {
        new InvestigationSnapshot(criticalClaims[0].ClaimId, "EvidenceSupportsClaim", now.AddDays(-9), now.AddDays(-5)),
        new InvestigationSnapshot(criticalClaims[1].ClaimId, "EvidenceSupportsClaim", now.AddHours(-20), now.AddHours(-10))
    },
    new[]
    {
        new DecisionSnapshot(criticalClaims[0].ClaimId, "RefundRecommended", "Approved"),
        new DecisionSnapshot(criticalClaims[1].ClaimId, "ChargebackRecommended", "Approved")
    },
    new[]
    {
        new ResolutionSnapshot(criticalClaims[0].ClaimId, "Refund", "Failed", 3),
        new ResolutionSnapshot(criticalClaims[1].ClaimId, "Chargeback", "ManualInterventionRequired", 3)
    }));

var critical = await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-CRITICAL", actor));
Check("critical subject score clamped", critical.Score == 100, ref passed);
Check("critical subject severity critical", critical.Severity == ProtectionSeverity.Critical, ref passed);
Check("critical subject recommendation escalate", critical.Recommendation == ProtectionRecommendation.EscalateOperations, ref passed);

// --------------------------------------------------
// Unknown subject.
// --------------------------------------------------
var missingBlocked = false;
try
{
    await service.EvaluateAsync(new EvaluateProtectionCommand("AWID-MISSING", "scenario-runner"));
}
catch (KeyNotFoundException)
{
    missingBlocked = true;
}
Check("unknown subject rejected", missingBlocked, ref passed);

// --------------------------------------------------
// Persistence and audit boundary proofs.
// --------------------------------------------------
var stored = await repository.GetLatestAsync("AWID-MERCHANT");
Check("finding persisted", stored is not null && stored.SubjectId == "AWID-MERCHANT", ref passed);

var events = await audit.GetAsync(merchant.FindingId);
Check("audit trail exists", events.Count >= 1, ref passed);
Check("automatic merchant blocking absent", events.All(x => x.Metadata["automaticMerchantBlockingPerformed"] == "false"), ref passed);
Check("automatic customer suspension absent", events.All(x => x.Metadata["automaticCustomerSuspensionPerformed"] == "false"), ref passed);
Check("refund execution absent", events.All(x => x.Metadata["refundExecutionPerformed"] == "false"), ref passed);
Check("money movement absent", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);
Check("ledger mutation absent", events.All(x => x.Metadata["ledgerMutationPerformed"] == "false"), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0018.6 dispute intelligence scenarios passed.");
Console.WriteLine("Dispute intelligence: IMPLEMENTED");
Console.WriteLine("Customer protection recommendations: IMPLEMENTED");
Console.WriteLine("Automatic merchant blocking: NOT IMPLEMENTED");
Console.WriteLine("Automatic customer suspension: NOT IMPLEMENTED");
Console.WriteLine("Refund execution: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IDisputeIntelligenceClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
