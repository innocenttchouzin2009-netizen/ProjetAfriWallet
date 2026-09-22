using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Application.Commands;
using AfriWallet.Merchants.Settlement.Application.Policies;
using AfriWallet.Merchants.Settlement.Application.Services;
using AfriWallet.Merchants.Settlement.Domain.Settlements;
using AfriWallet.Merchants.Settlement.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-settlement-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<MerchantSettlementDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

var now = new DateTimeOffset(2026, 9, 22, 19, 30, 0, TimeSpan.Zero);
var decisionId = Guid.NewGuid();
var intentId = Guid.NewGuid();
var settlementId = Guid.Empty;

try
{
    await using (var db = new MerchantSettlementDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();

        var captures = new EfMerchantCaptureSnapshotStore(db);
        var repository = new EfMerchantSettlementRepository(db);
        var audit = new EfMerchantSettlementAuditStore(db);
        var reader = new DurableCaptureEligibleDecisionReader(captures);
        var provider = new SandboxMerchantSettlementProvider();

        var capture = new CaptureEligibleDecisionSnapshot(
            decisionId,
            intentId,
            "AFM-DURABLE",
            "CaptureEligible",
            "Approved",
            125_000,
            "XAF",
            "Active",
            "Verified");

        await captures.SaveAsync(capture);
        Check("capture snapshot persisted", (await captures.GetAsync(decisionId)) == capture, ref passed);

        var service = new MerchantSettlementService(
            repository,
            reader,
            provider,
            audit,
            new FixedClock(now),
            new MerchantSettlementRoutingPolicy(),
            new MerchantSettlementRetryPolicy(),
            captures);

        var created = await service.CreateAsync(
            new CreateMerchantSettlementCommand(
                decisionId,
                MerchantSettlementRoute.MerchantSettlement,
                "durable-idem-1",
                "scenario-runner"));

        settlementId = created.SettlementId;
        Check("durable settlement created", created.Status == MerchantSettlementStatus.Created, ref passed);

        provider.Enqueue(MerchantSettlementProviderStatus.Accepted);
        var acknowledged = await service.DispatchAsync(
            new DispatchMerchantSettlementCommand(settlementId, "scenario-runner"));
        Check("delivery attempt persisted in aggregate", acknowledged.AttemptCount == 1, ref passed);
        Check("provider acknowledgement persisted", acknowledged.Status == MerchantSettlementStatus.Acknowledged, ref passed);

        var completed = await service.CompleteAsync(
            new CompleteMerchantSettlementCommand(settlementId, "scenario-runner"));
        Check("settlement completes before restart", completed.Status == MerchantSettlementStatus.Completed, ref passed);

        var events = await audit.GetAsync(settlementId);
        Check("audit persisted before restart", events.Count >= 3, ref passed);
    }

    await using (var db = new MerchantSettlementDbContext(options))
    {
        var captures = new EfMerchantCaptureSnapshotStore(db);
        var repository = new EfMerchantSettlementRepository(db);
        var audit = new EfMerchantSettlementAuditStore(db);
        var reader = new DurableCaptureEligibleDecisionReader(captures);
        var provider = new SandboxMerchantSettlementProvider();
        var service = new MerchantSettlementService(
            repository,
            reader,
            provider,
            audit,
            new FixedClock(now.AddMinutes(5)),
            new MerchantSettlementRoutingPolicy(),
            new MerchantSettlementRetryPolicy(),
            captures);

        var restored = await repository.GetAsync(settlementId);
        Check("settlement survives restart", restored is not null, ref passed);
        Check("terminal status survives restart", restored?.Status == MerchantSettlementStatus.Completed, ref passed);
        Check("attempt ledger survives restart", restored?.AttemptCount == 1, ref passed);
        Check("provider reference survives restart", !string.IsNullOrWhiteSpace(restored?.ProviderReference), ref passed);

        var capture = await captures.GetAsync(decisionId);
        Check("capture handoff survives restart", capture is not null && capture.PaymentIntentId == intentId, ref passed);

        var replayByKey = await service.CreateAsync(
            new CreateMerchantSettlementCommand(
                decisionId,
                MerchantSettlementRoute.MerchantSettlement,
                "durable-idem-1",
                "scenario-runner"));
        Check("idempotency key survives restart", replayByKey.SettlementId == settlementId, ref passed);

        var replayByDecision = await service.CreateAsync(
            new CreateMerchantSettlementCommand(
                decisionId,
                MerchantSettlementRoute.MerchantPayout,
                "different-key",
                "scenario-runner"));
        Check("decision uniqueness survives restart", replayByDecision.SettlementId == settlementId, ref passed);

        var events = await audit.GetAsync(settlementId);
        Check("audit survives restart", events.Count >= 3, ref passed);
        Check("audit preserves no-real-funds boundary",
            events.All(x => x.Metadata.TryGetValue("merchantFundsMoved", out var moved) && moved == "false"),
            ref passed);
    }

    Console.WriteLine($"\nChecks: {passed}\nPassed: {passed}\nFailed: 0\n");
    Console.WriteLine("AFW-BE-MERCHANT-SETTLEMENT-1 durable capture-to-settlement scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class FixedClock(DateTimeOffset now) : IMerchantSettlementClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
