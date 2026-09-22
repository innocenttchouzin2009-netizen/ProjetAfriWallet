using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Fx.Application;
using AfriWallet.Fx.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using Microsoft.EntityFrameworkCore;
using Settlement.Application.Interfaces;
using Settlement.Application.Services;
using Settlement.Domain.Instructions;
using Settlement.Infrastructure.Gateways;
using Settlement.Infrastructure.Persistence;
using Settlement.Infrastructure.Providers;
using Settlement.Infrastructure.Repositories;
using Treasury.Application.Interfaces;
using Treasury.Domain.Accounts;
using Treasury.Infrastructure.Persistence;
using Treasury.Infrastructure.Repositories;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var root = Path.Combine(Path.GetTempPath(), $"afwal-fx-clearing-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
var ledgerPath = Path.Combine(root, "ledger.db");
var settlementPath = Path.Combine(root, "settlement.db");
var treasuryPath = Path.Combine(root, "treasury.db");

try
{
    var ledgerOptions = new DbContextOptionsBuilder<LedgerDbContext>()
        .UseSqlite($"Data Source={ledgerPath}")
        .Options;
    var settlementOptions = new DbContextOptionsBuilder<SettlementDbContext>()
        .UseSqlite($"Data Source={settlementPath}")
        .Options;
    var treasuryOptions = new DbContextOptionsBuilder<TreasuryDbContext>()
        .UseSqlite($"Data Source={treasuryPath}")
        .Options;

    await using var ledgerDb = new LedgerDbContext(ledgerOptions);
    await using var settlementDb = new SettlementDbContext(settlementOptions);
    await using var treasuryDb = new TreasuryDbContext(treasuryOptions);
    await ledgerDb.Database.EnsureCreatedAsync();
    await settlementDb.Database.EnsureCreatedAsync();
    await treasuryDb.Database.EnsureCreatedAsync();

    var journalRepository = new EfJournalRepository(ledgerDb);
    var ledgerPosting = new LedgerPostingApplicationService(journalRepository);
    var balanceRead = new LedgerBackedBalanceReadService(
        new EfLedgerJournalReader(ledgerDb),
        new BalanceProjectionService());
    var treasuryRepository = new EfTreasuryRepository(treasuryDb);
    var timeProvider = new FixedTimeProvider(
        new DateTimeOffset(2026, 9, 22, 18, 0, 0, TimeSpan.Zero));

    var eurClearing = new TreasuryAccount(
        Guid.NewGuid(),
        "FX-CLEARING-EUR",
        "EUR FX Clearing",
        "EUR",
        TreasuryAccountType.Clearing);
    var xafClearing = new TreasuryAccount(
        Guid.NewGuid(),
        "FX-CLEARING-XAF",
        "XAF FX Clearing",
        "XAF",
        TreasuryAccountType.Clearing);
    await treasuryRepository.AddAccountAsync(eurClearing, CancellationToken.None);
    await treasuryRepository.AddAccountAsync(xafClearing, CancellationToken.None);

    var settlementRepository = new EfSettlementRepository(settlementDb);
    var coreFxProvider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)],
        timeProvider);
    var fxAdapter = new CoreFxQuoteProviderAdapter(
        new FxQuoteApplicationService(coreFxProvider));
    var gateway = new LedgerBackedTreasurySettlementGateway(
        ledgerPosting,
        balanceRead,
        treasuryRepository,
        timeProvider);
    var service = new MultiCurrencySettlementService(
        settlementRepository,
        fxAdapter,
        gateway);

    var source = Guid.NewGuid();
    var destination = Guid.NewGuid();

    var seed = await ledgerPosting.PostAsync(
        new PostJournalCommand(
            "EUR",
            "fx-clearing-seed",
            Guid.NewGuid(),
            [
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Debit, 1_000, "Seed source"),
                new PostLedgerLineCommand(source, LedgerSide.Credit, 1_000, "Fund settlement source")
            ]),
        timeProvider.GetUtcNow());
    Assert(seed.Succeeded, "Seed funding must succeed.");

    var instruction = await service.CreateInstructionAsync(
        source,
        destination,
        "EUR",
        "XAF",
        100,
        CancellationToken.None);

    Assert(instruction.AppliedQuote is not null, "Cross-currency settlement must freeze an FX quote.");
    Assert(instruction.AppliedQuote!.Rate == 655.957m, "Frozen FX rate mismatch.");
    Assert(instruction.DestinationAmountMinor == 65_596, "FX converted destination amount mismatch.");

    var executed = await service.ExecuteInstructionAsync(
        instruction.InstructionId,
        CancellationToken.None);
    Assert(executed.Status == SettlementInstructionStatus.Settled, "Cross-currency settlement must settle.");

    var journalsAfterFirstExecution = await ledgerDb.JournalEntries.AsNoTracking().CountAsync();
    Assert(journalsAfterFirstExecution == 3, "Cross-currency settlement must add exactly two ledger journals.");

    var sourceBalance = await balanceRead.ReadAsync(
        new BalanceKey(new AccountId(source), "EUR"));
    var sourceClearingBalance = await balanceRead.ReadAsync(
        new BalanceKey(new AccountId(eurClearing.AccountId), "EUR"));
    var destinationClearingBalance = await balanceRead.ReadAsync(
        new BalanceKey(new AccountId(xafClearing.AccountId), "XAF"));
    var destinationBalance = await balanceRead.ReadAsync(
        new BalanceKey(new AccountId(destination), "XAF"));

    Assert(sourceBalance.NetMinor == 900, "Source EUR account must be debited once.");
    Assert(sourceClearingBalance.NetMinor == 100, "EUR clearing account must receive source leg.");
    Assert(destinationClearingBalance.NetMinor == -65_596, "XAF clearing account must fund destination leg.");
    Assert(destinationBalance.NetMinor == 65_596, "Destination XAF account must be credited once.");

    await gateway.PostSettlementAsync(
        new TreasurySettlementPosting(
            instruction.InstructionId,
            instruction.SourceAccountId,
            instruction.DestinationAccountId,
            instruction.SourceCurrency,
            instruction.DestinationCurrency,
            instruction.SourceAmountMinor,
            instruction.DestinationAmountMinor,
            instruction.AppliedQuote.Rate),
        CancellationToken.None);

    var journalsAfterDuplicateGateway = await ledgerDb.JournalEntries.AsNoTracking().CountAsync();
    Assert(journalsAfterDuplicateGateway == 3, "Duplicate gateway execution must not create duplicate journals.");

    var duplicateService = await service.ExecuteInstructionAsync(
        instruction.InstructionId,
        CancellationToken.None);
    Assert(duplicateService.Status == SettlementInstructionStatus.Settled, "Duplicate service execution must remain settled.");
    Assert(await ledgerDb.JournalEntries.AsNoTracking().CountAsync() == 3, "Duplicate service execution must not repost.");

    var mismatchRejected = false;
    try
    {
        await gateway.PostSettlementAsync(
            new TreasurySettlementPosting(
                Guid.NewGuid(),
                source,
                destination,
                "EUR",
                "XAF",
                100,
                65_595,
                655.957m),
            CancellationToken.None);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("applied FX quote", StringComparison.Ordinal))
    {
        mismatchRejected = true;
    }
    Assert(mismatchRejected, "Destination amount must match the frozen FX rate.");

    var usdInstruction = await service.CreateInstructionAsync(
        source,
        Guid.NewGuid(),
        "EUR",
        "XAF",
        10,
        CancellationToken.None);

    var xafClearingRow = await treasuryDb.Accounts.SingleAsync(x => x.AccountId == xafClearing.AccountId);
    xafClearingRow.Status = (int)TreasuryAccountStatus.Suspended;
    await treasuryDb.SaveChangesAsync();

    var invalidClearingRejected = false;
    try
    {
        await gateway.PostSettlementAsync(
            new TreasurySettlementPosting(
                usdInstruction.InstructionId,
                usdInstruction.SourceAccountId,
                usdInstruction.DestinationAccountId,
                usdInstruction.SourceCurrency,
                usdInstruction.DestinationCurrency,
                usdInstruction.SourceAmountMinor,
                usdInstruction.DestinationAmountMinor,
                usdInstruction.AppliedQuote!.Rate),
            CancellationToken.None);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("invalid or inactive", StringComparison.Ordinal))
    {
        invalidClearingRejected = true;
    }
    Assert(invalidClearingRejected, "Inactive clearing account must fail closed.");

    var persisted = await settlementRepository.GetInstructionAsync(
        instruction.InstructionId,
        CancellationToken.None);
    Assert(persisted?.Status == SettlementInstructionStatus.Settled, "Settled instruction must remain durable.");

    Console.WriteLine("AFW-BE-FX-CLEARING-1 Settlement.FxClearing.Scenarios: PASS");
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
