using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Fx.Application;
using AfriWallet.Fx.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using Microsoft.EntityFrameworkCore;
using Settlement.Application.Services;
using Settlement.Domain.Instructions;
using Settlement.Infrastructure.Gateways;
using Settlement.Infrastructure.Persistence;
using Settlement.Infrastructure.Providers;
using Settlement.Infrastructure.Repositories;
using Treasury.Infrastructure.Repositories;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var root = Path.Combine(Path.GetTempPath(), $"afwal-settlement-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
var ledgerPath = Path.Combine(root, "ledger.db");
var settlementPath = Path.Combine(root, "settlement.db");

try
{
    var ledgerOptions = new DbContextOptionsBuilder<LedgerDbContext>()
        .UseSqlite($"Data Source={ledgerPath}")
        .Options;
    await using var ledgerDb = new LedgerDbContext(ledgerOptions);
    await ledgerDb.Database.EnsureCreatedAsync();

    var settlementOptions = new DbContextOptionsBuilder<SettlementDbContext>()
        .UseSqlite($"Data Source={settlementPath}")
        .Options;
    await using var settlementDb = new SettlementDbContext(settlementOptions);
    await settlementDb.Database.EnsureCreatedAsync();

    var journalRepository = new EfJournalRepository(ledgerDb);
    var ledgerPosting = new LedgerPostingApplicationService(journalRepository);
    var journalReader = new EfLedgerJournalReader(ledgerDb);
    var balanceRead = new LedgerBackedBalanceReadService(journalReader, new BalanceProjectionService());
    var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));

    var repository = new EfSettlementRepository(settlementDb);
    var coreFxProvider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)],
        timeProvider);
    var fxAdapter = new CoreFxQuoteProviderAdapter(new FxQuoteApplicationService(coreFxProvider));
    var treasuryRepository = new InMemoryTreasuryRepository();
    var treasury = new LedgerBackedTreasurySettlementGateway(ledgerPosting, balanceRead, treasuryRepository, timeProvider);
    var service = new MultiCurrencySettlementService(repository, fxAdapter, treasury);

    var source = Guid.NewGuid();
    var destination = Guid.NewGuid();
    var seedCorrelation = Guid.NewGuid();

    var seed = await ledgerPosting.PostAsync(
        new PostJournalCommand(
            "EUR",
            "settlement-seed",
            seedCorrelation,
            [
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Debit, 10_000, "Seed funding source"),
                new PostLedgerLineCommand(source, LedgerSide.Credit, 10_000, "Seed settlement account")
            ]),
        timeProvider.GetUtcNow());

    Assert(seed.Succeeded, "Seed ledger posting must succeed.");

    var instruction = await service.CreateInstructionAsync(
        source,
        destination,
        "EUR",
        "EUR",
        2_500,
        CancellationToken.None);

    var executed = await service.ExecuteInstructionAsync(instruction.InstructionId, CancellationToken.None);
    Assert(executed.Status == SettlementInstructionStatus.Settled, "Same-currency settlement must settle.");

    var sourceBalance = await balanceRead.ReadAsync(new BalanceKey(new AccountId(source), "EUR"));
    var destinationBalance = await balanceRead.ReadAsync(new BalanceKey(new AccountId(destination), "EUR"));
    Assert(sourceBalance.NetMinor == 7_500, "Source balance must be debited once.");
    Assert(destinationBalance.NetMinor == 2_500, "Destination balance must be credited once.");

    var duplicate = await service.ExecuteInstructionAsync(instruction.InstructionId, CancellationToken.None);
    Assert(duplicate.Status == SettlementInstructionStatus.Settled, "Duplicate execution must remain settled.");

    var journal = await ledgerPosting.GetByCorrelationIdAsync(instruction.InstructionId);
    Assert(journal.Succeeded && journal.Value is not null, "Settlement ledger journal must be correlated by instruction id.");

    var reloaded = await repository.GetInstructionAsync(instruction.InstructionId, CancellationToken.None);
    Assert(reloaded?.Status == SettlementInstructionStatus.Settled, "Settlement state must survive repository reload.");

    var insufficient = await service.CreateInstructionAsync(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "EUR",
        "EUR",
        1_000,
        CancellationToken.None);
    var rejected = await service.ExecuteInstructionAsync(insufficient.InstructionId, CancellationToken.None);
    Assert(rejected.Status == SettlementInstructionStatus.Rejected, "Insufficient funds must reject the settlement.");

    var crossCurrency = await service.CreateInstructionAsync(
        source,
        Guid.NewGuid(),
        "EUR",
        "XAF",
        100,
        CancellationToken.None);
    Assert(crossCurrency.AppliedQuote is not null, "Cross-currency instruction must use the core FX adapter.");

    var missingClearingRejected = false;
    try
    {
        await service.ExecuteInstructionAsync(crossCurrency.InstructionId, CancellationToken.None);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("FX clearing account", StringComparison.Ordinal))
    {
        missingClearingRejected = true;
    }
    Assert(missingClearingRejected, "Cross-currency settlement must fail closed when clearing accounts are not configured.");

    Console.WriteLine("AFW-BE-SETTLEMENT-1 Settlement.LedgerBacked.Scenarios: PASS");
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
