using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;
using AfriWallet.Merchants.Payout.Infrastructure;
using Microsoft.EntityFrameworkCore;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-64} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-payout-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<MerchantPayoutDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;
var clock = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 20, 0, 0, TimeSpan.Zero));
var provider = new IdempotentSandboxMerchantPayoutProvider();

try
{
    Guid payoutId;
    var receivableId = Guid.NewGuid();
    Guid destinationId;

    await using (var db = new MerchantPayoutDbContext(options))
    {
        await db.Database.EnsureCreatedAsync();

        var receivables = new EfMerchantReceivableStore(db);
        await receivables.SaveAsync(new MerchantReceivableSnapshot(
            receivableId,
            "AFM-100",
            12500,
            "XAF",
            "Captured",
            true,
            "CAPTURE-REF-1"));

        var service = Service(db, provider, clock);
        var destination = await service.RegisterDestinationAsync(
            new RegisterMerchantPayoutDestinationCommand(
                "AFM-100",
                MerchantPayoutDestinationType.MobileMoney,
                "momo-destination-ref-1",
                "XAF"));
        destinationId = destination.DestinationId;

        var first = await service.ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                receivableId,
                destinationId,
                "payout-key-1",
                "scenario"));

        payoutId = first.PayoutId;
        Check("payout execution succeeds", first.Status == MerchantPayoutStatus.Succeeded, ref passed);
        Check("provider reference persisted", !string.IsNullOrWhiteSpace(first.ProviderReference), ref passed);
        Check("provider called once", provider.Calls == 1, ref passed);

        var events = await new EfMerchantPayoutAuditStore(db).ListAsync(first.PayoutId);
        Check("payout audit created/completed", events.Count == 2, ref passed);
        Check("real external payout remains false", events.All(x => x.Metadata["realExternalPayoutPerformed"] == "false"), ref passed);
        Check("ledger mutation remains false", events.All(x => x.Metadata["ledgerMutationPerformed"] == "false"), ref passed);
        Check("money movement remains false", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);

        var replay = await service.ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                receivableId,
                destinationId,
                "payout-key-1",
                "scenario-replay"));
        Check("same key returns same payout", replay.PayoutId == first.PayoutId, ref passed);
        Check("terminal replay does not call provider again", provider.Calls == 1, ref passed);
    }

    await using (var db = new MerchantPayoutDbContext(options))
    {
        var service = Service(db, provider, clock);
        var replayAfterRestart = await service.ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                receivableId,
                destinationId,
                "payout-key-1",
                "scenario-restart"));

        Check("restart preserves payout id", replayAfterRestart.PayoutId == payoutId, ref passed);
        Check("restart does not duplicate provider call", provider.Calls == 1, ref passed);
    }

    await using (var db = new MerchantPayoutDbContext(options))
    {
        var foreignDestination = MerchantPayoutDestination.Create(
            "AFM-OTHER",
            MerchantPayoutDestinationType.BankAccount,
            "bank-account-ref-foreign",
            "XAF",
            clock.GetUtcNow());
        await new EfMerchantPayoutDestinationRepository(db).AddAsync(foreignDestination);

        var foreignReceivableId = Guid.NewGuid();
        await new EfMerchantReceivableStore(db).SaveAsync(new MerchantReceivableSnapshot(
            foreignReceivableId,
            "AFM-100",
            5000,
            "XAF",
            "Captured",
            true,
            null));

        var blocked = false;
        try
        {
            await Service(db, provider, clock).ExecuteAsync(
                new ExecuteMerchantPayoutCommand(
                    foreignReceivableId,
                    foreignDestination.DestinationId,
                    "payout-key-foreign",
                    "scenario"));
        }
        catch (InvalidOperationException)
        {
            blocked = true;
        }
        Check("foreign merchant destination blocked", blocked, ref passed);
    }

    await using (var db = new MerchantPayoutDbContext(options))
    {
        var mismatchReceivableId = Guid.NewGuid();
        await new EfMerchantReceivableStore(db).SaveAsync(new MerchantReceivableSnapshot(
            mismatchReceivableId,
            "AFM-200",
            8000,
            "EUR",
            "Captured",
            true,
            null));

        var destination = MerchantPayoutDestination.Create(
            "AFM-200",
            MerchantPayoutDestinationType.AfWalWallet,
            "wallet-ref-200",
            "XAF",
            clock.GetUtcNow());
        await new EfMerchantPayoutDestinationRepository(db).AddAsync(destination);

        var blocked = false;
        try
        {
            await Service(db, provider, clock).ExecuteAsync(
                new ExecuteMerchantPayoutCommand(
                    mismatchReceivableId,
                    destination.DestinationId,
                    "payout-key-currency",
                    "scenario"));
        }
        catch (InvalidOperationException)
        {
            blocked = true;
        }
        Check("currency mismatch blocked", blocked, ref passed);
    }

    await using (var db = new MerchantPayoutDbContext(options))
    {
        var pendingReceivableId = Guid.NewGuid();
        await new EfMerchantReceivableStore(db).SaveAsync(new MerchantReceivableSnapshot(
            pendingReceivableId,
            "AFM-300",
            9000,
            "USD",
            "Processing",
            false,
            null));

        var destination = MerchantPayoutDestination.Create(
            "AFM-300",
            MerchantPayoutDestinationType.BankAccount,
            "bank-account-ref-300",
            "USD",
            clock.GetUtcNow());
        await new EfMerchantPayoutDestinationRepository(db).AddAsync(destination);

        var blocked = false;
        try
        {
            await Service(db, provider, clock).ExecuteAsync(
                new ExecuteMerchantPayoutCommand(
                    pendingReceivableId,
                    destination.DestinationId,
                    "payout-key-not-ready",
                    "scenario"));
        }
        catch (InvalidOperationException)
        {
            blocked = true;
        }
        Check("non-captured receivable blocked", blocked, ref passed);
    }

    await using (var db = new MerchantPayoutDbContext(options))
    {
        var resumeReceivableId = Guid.NewGuid();
        var destination = MerchantPayoutDestination.Create(
            "AFM-400",
            MerchantPayoutDestinationType.MobileMoney,
            "momo-destination-ref-400",
            "XAF",
            clock.GetUtcNow());

        await new EfMerchantReceivableStore(db).SaveAsync(new MerchantReceivableSnapshot(
            resumeReceivableId,
            "AFM-400",
            15000,
            "XAF",
            "Captured",
            true,
            "CAPTURE-REF-400"));
        await new EfMerchantPayoutDestinationRepository(db).AddAsync(destination);

        var processing = MerchantPayoutExecution.Create(
            resumeReceivableId,
            "AFM-400",
            15000,
            "XAF",
            destination.DestinationId,
            "payout-key-resume",
            clock.GetUtcNow());
        processing.Start(clock.GetUtcNow());
        await new EfMerchantPayoutRepository(db).AddAsync(processing);

        _ = await provider.ExecuteAsync(new MerchantPayoutProviderRequest(
            processing.PayoutId,
            processing.ReceivableId,
            processing.MerchantId,
            processing.AmountMinor,
            processing.Currency,
            destination.Type,
            destination.Reference,
            processing.IdempotencyKey));

        var callsBeforeResume = provider.Calls;
        var resumed = await Service(db, provider, clock).ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                resumeReceivableId,
                destination.DestinationId,
                "payout-key-resume",
                "scenario-recovery"));

        Check("processing payout resumes to success", resumed.Status == MerchantPayoutStatus.Succeeded, ref passed);
        Check("provider idempotency prevents duplicate side effect", provider.Calls == callsBeforeResume, ref passed);
    }

    await using (var db = new MerchantPayoutDbContext(options))
    {
        var failureReceivableId = Guid.NewGuid();
        var destination = MerchantPayoutDestination.Create(
            "AFM-500",
            MerchantPayoutDestinationType.BankAccount,
            "bank-account-ref-500",
            "EUR",
            clock.GetUtcNow());
        await new EfMerchantReceivableStore(db).SaveAsync(new MerchantReceivableSnapshot(
            failureReceivableId,
            "AFM-500",
            22000,
            "EUR",
            "Captured",
            true,
            null));
        await new EfMerchantPayoutDestinationRepository(db).AddAsync(destination);

        var failing = new FailingPayoutProvider();
        var service = Service(db, failing, clock);
        var failed = await service.ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                failureReceivableId,
                destination.DestinationId,
                "payout-key-failure",
                "scenario"));

        Check("provider failure persisted terminally",
            failed.Status == MerchantPayoutStatus.Failed && failed.FailureCode == "provider_declined",
            ref passed);

        var replay = await service.ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                failureReceivableId,
                destination.DestinationId,
                "payout-key-failure",
                "scenario-replay"));
        Check("failed payout is idempotent", replay.PayoutId == failed.PayoutId && failing.Calls == 1, ref passed);
    }

    Console.WriteLine();
    Console.WriteLine($"Checks: {passed}");
    Console.WriteLine($"Passed: {passed}");
    Console.WriteLine("Failed: 0");
    Console.WriteLine("Skipped: 0");
    Console.WriteLine("AFW-BE-MERCHANT-PAYOUT-1 durable merchant payout execution foundation scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

static MerchantPayoutService Service(
    MerchantPayoutDbContext db,
    IMerchantPayoutProvider provider,
    TimeProvider clock) =>
    new(
        new EfMerchantReceivableStore(db),
        new EfMerchantPayoutDestinationRepository(db),
        new EfMerchantPayoutRepository(db),
        provider,
        new EfMerchantPayoutAuditStore(db),
        clock);

sealed class FailingPayoutProvider : IMerchantPayoutProvider
{
    public int Calls { get; private set; }

    public Task<MerchantPayoutProviderResult> ExecuteAsync(
        MerchantPayoutProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new MerchantPayoutProviderResult(false, null, "provider_declined"));
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
