using AfriWallet.Merchants.Receivables.Application;
using AfriWallet.Merchants.Receivables.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-62} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 9, 22, 20, 0, 0, TimeSpan.Zero);
var clock = new FixedTimeProvider(now);
var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-receivables-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<MerchantReceivablesDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

var merchantId = "AFM-RECV-001";
var captureId = Guid.NewGuid();
var decisionId = Guid.NewGuid();
var intentId = Guid.NewGuid();
var captures = new ScenarioCaptureReader();
var merchants = new ScenarioMerchantReader();
captures.Set(new(captureId, decisionId, intentId, merchantId, 100_000, "XOF", "Captured", "CAP-001"));
merchants.Set(new(merchantId, "Active", "XOF"));

try
{
    await using (var db = new MerchantReceivablesDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();
        var fees = new EfMerchantFeeScheduleStore(db);
        var repo = new EfMerchantReceivableRepository(db);
        var audit = new EfMerchantReceivableAuditStore(db);
        var service = new MerchantReceivableService(captures, merchants, fees, repo, audit, clock);

        var schedule = await service.ConfigureFeeScheduleAsync(new(merchantId, "XOF", 150, 100, "scenario"));
        Check("fee schedule basis points configured", schedule.PercentageBasisPoints == 150, ref passed);

        var created = await service.CreateFromCaptureAsync(new(captureId, "receivable-key-1", "scenario"));
        Check("gross amount captured", created.GrossAmountMinor == 100_000, ref passed);
        Check("fee calculation deterministic", created.FeeAmountMinor == 1_600, ref passed);
        Check("net receivable calculated", created.NetAmountMinor == 98_400, ref passed);

        var repeated = await service.CreateFromCaptureAsync(new(captureId, "receivable-key-1", "scenario"));
        Check("same idempotency key returns same receivable", repeated.ReceivableId == created.ReceivableId, ref passed);

        var open = await service.ListOpenAsync(merchantId, "XOF");
        Check("open receivable listed", open.Count == 1, ref passed);

        var mismatchRejected = false;
        try
        {
            await service.ApplySettlementReceiptAsync(new(created.ReceivableId, Guid.NewGuid(), 100_000, "XOF", "scenario"));
        }
        catch (InvalidOperationException)
        {
            mismatchRejected = true;
        }
        Check("gross settlement receipt rejected", mismatchRejected, ref passed);

        clock.Advance(TimeSpan.FromMinutes(5));
        var settlementId = Guid.NewGuid();
        var settled = await service.ApplySettlementReceiptAsync(new(created.ReceivableId, settlementId, 98_400, "XOF", "scenario"));
        Check("net settlement receipt closes receivable", settled.Status.ToString() == "Settled", ref passed);
        Check("settlement id retained", settled.SettlementId == settlementId, ref passed);

        var events = await audit.ListAsync(created.ReceivableId);
        Check("receivable audit contains create and settle", events.Count == 2, ref passed);
        Check("audit proves no money movement", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);
    }

    await using (var restarted = new MerchantReceivablesDbContext(options))
    {
        var repo = new EfMerchantReceivableRepository(restarted);
        var durable = await repo.GetByCaptureAsync(captureId);
        Check("receivable survives process restart", durable?.SettlementId is not null, ref passed);
    }

    await using (var db = new MerchantReceivablesDbContext(options))
    {
        var service = new MerchantReceivableService(
            captures,
            merchants,
            new EfMerchantFeeScheduleStore(db),
            new EfMerchantReceivableRepository(db),
            new EfMerchantReceivableAuditStore(db),
            clock);

        var failedCaptureId = Guid.NewGuid();
        captures.Set(new(failedCaptureId, Guid.NewGuid(), Guid.NewGuid(), merchantId, 50_000, "XOF", "Failed", null));
        var failedBlocked = false;
        try
        {
            await service.CreateFromCaptureAsync(new(failedCaptureId, "failed-key", "scenario"));
        }
        catch (InvalidOperationException)
        {
            failedBlocked = true;
        }
        Check("failed capture cannot create receivable", failedBlocked, ref passed);

        var inactiveCaptureId = Guid.NewGuid();
        captures.Set(new(inactiveCaptureId, Guid.NewGuid(), Guid.NewGuid(), "AFM-INACTIVE", 50_000, "XOF", "Captured", "CAP-2"));
        merchants.Set(new("AFM-INACTIVE", "Suspended", "XOF"));
        await service.ConfigureFeeScheduleAsync(new("AFM-INACTIVE", "XOF", 100, 0, "scenario"));
        var inactiveBlocked = false;
        try
        {
            await service.CreateFromCaptureAsync(new(inactiveCaptureId, "inactive-key", "scenario"));
        }
        catch (InvalidOperationException)
        {
            inactiveBlocked = true;
        }
        Check("inactive merchant cannot create receivable", inactiveBlocked, ref passed);

        var mismatchCaptureId = Guid.NewGuid();
        captures.Set(new(mismatchCaptureId, Guid.NewGuid(), Guid.NewGuid(), "AFM-MISMATCH", 50_000, "EUR", "Captured", "CAP-3"));
        merchants.Set(new("AFM-MISMATCH", "Active", "XOF"));
        var currencyBlocked = false;
        try
        {
            await service.CreateFromCaptureAsync(new(mismatchCaptureId, "currency-key", "scenario"));
        }
        catch (InvalidOperationException)
        {
            currencyBlocked = true;
        }
        Check("capture/settlement currency mismatch blocked", currencyBlocked, ref passed);
    }

    Console.WriteLine();
    Console.WriteLine($"AFW-BE-MERCHANT-RECEIVABLES-1 scenarios: PASS ({passed})");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class ScenarioCaptureReader : IMerchantCaptureReceivableReader
{
    private readonly Dictionary<Guid, CapturedMerchantPaymentSnapshot> items = new();
    public void Set(CapturedMerchantPaymentSnapshot value) => items[value.CaptureExecutionId] = value;
    public Task<CapturedMerchantPaymentSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(id, out var value);
        return Task.FromResult(value);
    }
}

sealed class ScenarioMerchantReader : IMerchantRegistryReceivableReader
{
    private readonly Dictionary<string, MerchantReceivableRegistrySnapshot> items = new(StringComparer.OrdinalIgnoreCase);
    public void Set(MerchantReceivableRegistrySnapshot value) => items[value.MerchantId] = value;
    public Task<MerchantReceivableRegistrySnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(merchantId, out var value);
        return Task.FromResult(value);
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset current = now;
    public override DateTimeOffset GetUtcNow() => current;
    public void Advance(TimeSpan value) => current = current.Add(value);
}
