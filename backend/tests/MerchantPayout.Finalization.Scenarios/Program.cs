using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;
using AfriWallet.Merchants.Payout.Infrastructure;
using AfriWallet.Merchants.Receivables.Application;
using AfriWallet.Merchants.Receivables.Domain;
using AfriWallet.Merchants.Receivables.Infrastructure;
using AfriWallet.Transfer.Application;
using Microsoft.EntityFrameworkCore;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-72} {(ok ? "PASS" : "FAIL")}");
    if (!ok) throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var payoutDbPath = Path.Combine(Path.GetTempPath(), $"afwal-payout-finalization-{Guid.NewGuid():N}.db");
var receivableDbPath = Path.Combine(Path.GetTempPath(), $"afwal-receivable-finalization-{Guid.NewGuid():N}.db");
var ledgerDbPath = Path.Combine(Path.GetTempPath(), $"afwal-ledger-finalization-{Guid.NewGuid():N}.db");

var payoutOptions = new DbContextOptionsBuilder<MerchantPayoutDbContext>()
    .UseSqlite($"Data Source={payoutDbPath}")
    .Options;
var receivableOptions = new DbContextOptionsBuilder<MerchantReceivablesDbContext>()
    .UseSqlite($"Data Source={receivableDbPath}")
    .Options;
var ledgerOptions = new DbContextOptionsBuilder<LedgerDbContext>()
    .UseSqlite($"Data Source={ledgerDbPath}")
    .Options;

var now = new DateTimeOffset(2026, 9, 23, 19, 0, 0, TimeSpan.Zero);
var clock = new FixedTimeProvider(now);
var provider = new IdempotentSandboxMerchantPayoutProvider();
var payoutClearingAccount = new AccountId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
var walletId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
var walletAccount = new AccountId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
var walletResolver = new ScenarioWalletLedgerAccountResolver(walletId, walletAccount);

try
{
    Guid payoutId;
    Guid receivableId;
    Guid destinationId;

    await using (var payoutDb = new MerchantPayoutDbContext(payoutOptions))
    await using (var receivableDb = new MerchantReceivablesDbContext(receivableOptions))
    await using (var ledgerDb = new LedgerDbContext(ledgerOptions))
    {
        await payoutDb.Database.EnsureCreatedAsync();
        await receivableDb.Database.EnsureCreatedAsync();
        await ledgerDb.Database.EnsureCreatedAsync();

        var receivableRepository = new EfMerchantReceivableRepository(receivableDb);
        var receivable = MerchantReceivable.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "AFM-FINALIZE-1",
            25_000,
            "XAF",
            new MerchantFeeSchedule("AFM-FINALIZE-1", "XAF", 0, 0),
            "receivable-finalize-1",
            now);
        await receivableRepository.AddAsync(receivable);
        receivableId = receivable.ReceivableId;

        var payoutReceivables = new EfMerchantReceivableStore(payoutDb);
        await payoutReceivables.SaveAsync(new MerchantReceivableSnapshot(
            receivable.ReceivableId,
            receivable.MerchantId,
            receivable.NetAmountMinor,
            receivable.Currency,
            "Captured",
            true,
            "CAPTURE-FINALIZE-1"));

        var destination = MerchantPayoutDestination.Create(
            receivable.MerchantId,
            MerchantPayoutDestinationType.AfWalWallet,
            walletId.ToString("D"),
            receivable.Currency,
            now);
        await new EfMerchantPayoutDestinationRepository(payoutDb).AddAsync(destination);
        destinationId = destination.DestinationId;

        var finalizer = Finalizer(receivableRepository, ledgerDb, walletResolver, payoutClearingAccount);
        var service = Service(payoutDb, provider, clock, finalizer);

        var result = await service.ExecuteAsync(new ExecuteMerchantPayoutCommand(
            receivable.ReceivableId,
            destination.DestinationId,
            "payout-finalize-1",
            "scenario"));

        payoutId = result.PayoutId;
        Check("AfWal wallet payout reaches Succeeded", result.Status == MerchantPayoutStatus.Succeeded, ref passed);

        var journal = await new LedgerPostingApplicationService(new EfJournalRepository(ledgerDb))
            .GetByCorrelationIdAsync(result.PayoutId);
        Check("PayoutId is the Ledger correlation id", journal.Succeeded && journal.Value is not null, ref passed);
        Check("Ledger debit uses configured payout clearing account",
            journal.Value!.Lines.Any(x => x.AccountId == payoutClearingAccount.Value && x.Side == LedgerSide.Debit && x.AmountMinor == receivable.NetAmountMinor),
            ref passed);
        Check("Ledger credit targets resolved AfWal wallet account",
            journal.Value.Lines.Any(x => x.AccountId == walletAccount.Value && x.Side == LedgerSide.Credit && x.AmountMinor == receivable.NetAmountMinor),
            ref passed);

        var settled = await receivableRepository.GetAsync(receivable.ReceivableId);
        Check("Receivable settlement receipt uses PayoutId",
            settled?.Status == MerchantReceivableStatus.Settled && settled.SettlementId == result.PayoutId,
            ref passed);

        var audit = await new EfMerchantPayoutAuditStore(payoutDb).ListAsync(result.PayoutId);
        var completed = audit.Single(x => x.EventType == "payout.completed");
        Check("Completed payout audit records Ledger mutation", completed.Metadata["ledgerMutationPerformed"] == "true", ref passed);
        Check("Completed payout audit records money movement", completed.Metadata["moneyMovementPerformed"] == "true", ref passed);

        var replay = await service.ExecuteAsync(new ExecuteMerchantPayoutCommand(
            receivable.ReceivableId,
            destination.DestinationId,
            "payout-finalize-1",
            "scenario-replay"));
        Check("Terminal replay returns same PayoutId", replay.PayoutId == result.PayoutId, ref passed);
        Check("Terminal replay does not call provider again", provider.Calls == 1, ref passed);
    }

    await using (var payoutDb = new MerchantPayoutDbContext(payoutOptions))
    await using (var ledgerDb = new LedgerDbContext(ledgerOptions))
    {
        var journals = await ledgerDb.JournalEntries.AsNoTracking()
            .Where(x => x.CorrelationId == payoutId)
            .CountAsync();
        Check("Terminal replay leaves exactly one Ledger journal", journals == 1, ref passed);
    }

    // Crash/replay: provider side effect and financial finalization happen, but payout stays Processing.
    await using (var payoutDb = new MerchantPayoutDbContext(payoutOptions))
    await using (var receivableDb = new MerchantReceivablesDbContext(receivableOptions))
    await using (var ledgerDb = new LedgerDbContext(ledgerOptions))
    {
        var receivableRepository = new EfMerchantReceivableRepository(receivableDb);
        var crashReceivable = MerchantReceivable.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "AFM-FINALIZE-CRASH",
            31_000,
            "XAF",
            new MerchantFeeSchedule("AFM-FINALIZE-CRASH", "XAF", 0, 0),
            "receivable-finalize-crash",
            now);
        await receivableRepository.AddAsync(crashReceivable);
        await new EfMerchantReceivableStore(payoutDb).SaveAsync(new MerchantReceivableSnapshot(
            crashReceivable.ReceivableId,
            crashReceivable.MerchantId,
            crashReceivable.NetAmountMinor,
            crashReceivable.Currency,
            "Captured",
            true,
            "CAPTURE-FINALIZE-CRASH"));

        var destination = MerchantPayoutDestination.Create(
            crashReceivable.MerchantId,
            MerchantPayoutDestinationType.AfWalWallet,
            walletId.ToString("D"),
            crashReceivable.Currency,
            now);
        await new EfMerchantPayoutDestinationRepository(payoutDb).AddAsync(destination);

        var processing = MerchantPayoutExecution.Create(
            crashReceivable.ReceivableId,
            crashReceivable.MerchantId,
            crashReceivable.NetAmountMinor,
            crashReceivable.Currency,
            destination.DestinationId,
            "payout-finalize-crash",
            now);
        processing.Start(now);
        await new EfMerchantPayoutRepository(payoutDb).AddAsync(processing);

        _ = await provider.ExecuteAsync(new MerchantPayoutProviderRequest(
            processing.PayoutId,
            processing.ReceivableId,
            processing.MerchantId,
            processing.AmountMinor,
            processing.Currency,
            destination.Type,
            destination.Reference,
            processing.IdempotencyKey));

        var finalizer = Finalizer(receivableRepository, ledgerDb, walletResolver, payoutClearingAccount);
        await finalizer.FinalizeAsync(new MerchantPayoutFinalizationRequest(
            processing.PayoutId,
            processing.ReceivableId,
            processing.MerchantId,
            processing.AmountMinor,
            processing.Currency,
            destination.Type,
            destination.Reference,
            "scenario-crash",
            now));

        var callsBeforeResume = provider.Calls;
        var resumed = await Service(payoutDb, provider, clock, finalizer).ExecuteAsync(
            new ExecuteMerchantPayoutCommand(
                processing.ReceivableId,
                destination.DestinationId,
                processing.IdempotencyKey,
                "scenario-recovery"));

        Check("Crash replay resumes Processing payout to Succeeded", resumed.Status == MerchantPayoutStatus.Succeeded, ref passed);
        Check("Provider idempotency prevents duplicate external side effect", provider.Calls == callsBeforeResume, ref passed);

        var journalCount = await ledgerDb.JournalEntries.AsNoTracking()
            .CountAsync(x => x.CorrelationId == processing.PayoutId);
        Check("Crash replay leaves exactly one Ledger journal", journalCount == 1, ref passed);

        var settled = await receivableRepository.GetAsync(processing.ReceivableId);
        Check("Crash replay preserves idempotent settlement receipt",
            settled?.Status == MerchantReceivableStatus.Settled && settled.SettlementId == processing.PayoutId,
            ref passed);
    }

    Console.WriteLine();
    Console.WriteLine($"Checks: {passed}");
    Console.WriteLine($"Passed: {passed}");
    Console.WriteLine("Failed: 0");
    Console.WriteLine("AFW-BE-MERCHANT-PAYOUT-FINALIZATION-1 scenarios: PASS");
}
finally
{
    foreach (var path in new[] { payoutDbPath, receivableDbPath, ledgerDbPath })
        if (File.Exists(path)) File.Delete(path);
}

static LedgerBackedAfWalWalletPayoutFinalizer Finalizer(
    IMerchantReceivableRepository receivables,
    LedgerDbContext ledgerDb,
    IWalletLedgerAccountResolver walletResolver,
    AccountId payoutClearingAccount) =>
    new(
        receivables,
        walletResolver,
        new LedgerPostingApplicationService(new EfJournalRepository(ledgerDb)),
        payoutClearingAccount);

static MerchantPayoutService Service(
    MerchantPayoutDbContext db,
    IMerchantPayoutProvider provider,
    TimeProvider clock,
    IMerchantPayoutFinalizer finalizer) =>
    new(
        new EfMerchantReceivableStore(db),
        new EfMerchantPayoutDestinationRepository(db),
        new EfMerchantPayoutRepository(db),
        provider,
        new EfMerchantPayoutAuditStore(db),
        clock,
        finalizer);

sealed class ScenarioWalletLedgerAccountResolver(Guid walletId, AccountId accountId)
    : IWalletLedgerAccountResolver
{
    public Task<AccountId?> ResolveAsync(Guid value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(value == walletId ? (AccountId?)accountId : null);
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
