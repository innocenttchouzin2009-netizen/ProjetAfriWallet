using AfriWallet.Merchants.Billing.Application;
using AfriWallet.Merchants.Billing.Domain;
using AfriWallet.Merchants.Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-66} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 9, 23, 19, 0, 0, TimeSpan.Zero);
var clock = new FixedTimeProvider(now);
var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-billing-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<MerchantBillingDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

var captureId = Guid.NewGuid();
var captures = new ScenarioCaptureReader();
captures.Set(new(captureId, "AFM-BILL-001", "Captured", true));
var receivables = new ScenarioReceivablePort();
receivables.SetMerchant(captureId, "AFM-BILL-001");

try
{
    Guid handoffId;
    Guid receivableId;

    await using (var db = new MerchantBillingDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();
        var repo = new EfMerchantBillingHandoffRepository(db);
        var audit = new EfMerchantBillingAuditStore(db);
        var service = new MerchantBillingService(captures, receivables, repo, audit, clock);

        var first = await service.ProcessCaptureAsync(new(captureId, "scenario"));
        handoffId = first.HandoffId;
        receivableId = first.ReceivableId ?? Guid.Empty;

        Check("captured payment produces completed billing handoff", first.Status == MerchantBillingHandoffStatus.Completed, ref passed);
        Check("billing handoff retains receivable id", receivableId != Guid.Empty, ref passed);
        Check("first handoff performs one receivable call", receivables.Calls == 1, ref passed);
        Check("first handoff records one attempt", first.AttemptCount == 1, ref passed);

        var repeated = await service.ProcessCaptureAsync(new(captureId, "scenario"));
        Check("completed handoff is idempotent", repeated.HandoffId == handoffId && repeated.ReceivableId == receivableId, ref passed);
        Check("completed handoff does not recreate receivable", receivables.Calls == 1, ref passed);

        var events = await audit.ListAsync(handoffId);
        Check("billing audit has created and completed events", events.Count == 2, ref passed);
        Check("billing audit proves no settlement", events.All(x => x.Metadata["settlementPerformed"] == "false"), ref passed);
        Check("billing audit proves no payout", events.All(x => x.Metadata["payoutPerformed"] == "false"), ref passed);
        Check("billing audit proves no money movement", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);
    }

    await using (var restarted = new MerchantBillingDbContext(options))
    {
        var repo = new EfMerchantBillingHandoffRepository(restarted);
        var durable = await repo.GetByCaptureAsync(captureId);
        Check("completed billing handoff survives restart", durable?.ReceivableId == receivableId, ref passed);
    }

    var retryCaptureId = Guid.NewGuid();
    captures.Set(new(retryCaptureId, "AFM-BILL-002", "Captured", true));
    receivables.SetMerchant(retryCaptureId, "AFM-BILL-002");
    receivables.FailNext = true;

    await using (var db = new MerchantBillingDbContext(options))
    {
        var service = new MerchantBillingService(
            captures,
            receivables,
            new EfMerchantBillingHandoffRepository(db),
            new EfMerchantBillingAuditStore(db),
            clock);

        var failed = false;
        try
        {
            await service.ProcessCaptureAsync(new(retryCaptureId, "scenario"));
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }
        Check("transient receivable failure leaves retryable handoff", failed, ref passed);
    }

    clock.Advance(TimeSpan.FromMinutes(1));

    await using (var restarted = new MerchantBillingDbContext(options))
    {
        var repo = new EfMerchantBillingHandoffRepository(restarted);
        var audit = new EfMerchantBillingAuditStore(restarted);
        var service = new MerchantBillingService(captures, receivables, repo, audit, clock);

        var retried = await service.ProcessCaptureAsync(new(retryCaptureId, "scenario"));
        Check("failed handoff resumes after restart", retried.Status == MerchantBillingHandoffStatus.Completed, ref passed);
        Check("retry increments attempt ledger", retried.AttemptCount == 2, ref passed);
        Check("retry uses deterministic receivable idempotency key", receivables.KeysFor(retryCaptureId).Distinct().Count() == 1, ref passed);

        var events = await audit.ListAsync(retried.HandoffId);
        Check("retry audit captures failed then completed lifecycle",
            events.Any(x => x.EventType == "billing.handoff.failed") &&
            events.Any(x => x.EventType == "billing.handoff.completed"), ref passed);
    }

    var failedCaptureId = Guid.NewGuid();
    captures.Set(new(failedCaptureId, "AFM-BILL-003", "Failed", false));
    await using (var db = new MerchantBillingDbContext(options))
    {
        var service = new MerchantBillingService(
            captures,
            receivables,
            new EfMerchantBillingHandoffRepository(db),
            new EfMerchantBillingAuditStore(db),
            clock);

        var blocked = false;
        try
        {
            await service.ProcessCaptureAsync(new(failedCaptureId, "scenario"));
        }
        catch (InvalidOperationException)
        {
            blocked = true;
        }
        Check("non-captured payment cannot enter billing", blocked, ref passed);
    }

    Console.WriteLine();
    Console.WriteLine($"AFW-BE-MERCHANT-BILLING-1 scenarios: PASS ({passed})");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class ScenarioCaptureReader : IMerchantBillingCaptureReader
{
    private readonly Dictionary<Guid, MerchantBillingCaptureSnapshot> values = new();
    public void Set(MerchantBillingCaptureSnapshot value) => values[value.CaptureExecutionId] = value;
    public Task<MerchantBillingCaptureSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id, out var value);
        return Task.FromResult(value);
    }
}

sealed class ScenarioReceivablePort : IMerchantBillingReceivablePort
{
    private readonly Dictionary<Guid, Guid> receivableIds = new();
    private readonly Dictionary<Guid, List<string>> keys = new();
    private readonly Dictionary<Guid, string> merchants = new();
    public int Calls { get; private set; }
    public bool FailNext { get; set; }

    public void SetMerchant(Guid captureId, string merchantId) => merchants[captureId] = merchantId;

    public IReadOnlyCollection<string> KeysFor(Guid captureId) =>
        keys.TryGetValue(captureId, out var value) ? value : Array.Empty<string>();

    public Task<MerchantBillingReceivableSnapshot> CreateFromCaptureAsync(
        Guid captureExecutionId,
        string idempotencyKey,
        string actor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        if (!keys.TryGetValue(captureExecutionId, out var list))
            keys[captureExecutionId] = list = [];
        list.Add(idempotencyKey);

        if (FailNext)
        {
            FailNext = false;
            throw new InvalidOperationException("simulated receivable failure");
        }

        if (!receivableIds.TryGetValue(captureExecutionId, out var id))
            receivableIds[captureExecutionId] = id = Guid.NewGuid();

        return Task.FromResult(new MerchantBillingReceivableSnapshot(
            id,
            captureExecutionId,
            merchants.TryGetValue(captureExecutionId, out var merchantId) ? merchantId : "UNKNOWN",
            100_000,
            1_600,
            98_400,
            "XOF",
            "Open"));
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset current = now;
    public override DateTimeOffset GetUtcNow() => current;
    public void Advance(TimeSpan value) => current = current.Add(value);
}
