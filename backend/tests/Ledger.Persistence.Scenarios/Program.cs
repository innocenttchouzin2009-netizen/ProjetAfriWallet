using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("migration applies and journal round-trips", MigrationAndRoundTripAsync),
    ("correlation id is unique in database", CorrelationIdIsUniqueAsync),
    ("ledger line order is preserved", LineOrderIsPreservedAsync)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

static async Task MigrationAndRoundTripAsync()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var repository = new EfJournalRepository(fixture.Context);
    var correlationId = Guid.NewGuid();
    var journal = CreateJournal(correlationId, 12500, 12500);

    await repository.AddAsync(journal);

    var restored = await repository.GetAsync(journal.Id);
    Assert(restored is not null, "Journal was not restored.");
    Assert(restored!.CorrelationId == correlationId, "Correlation id changed.");
    Assert(restored.CurrencyCode == "XAF", "Currency changed.");
    Assert(restored.Lines.Count == 2, "Ledger lines were not restored.");
    Assert(await repository.ExistsByCorrelationIdAsync(correlationId), "Correlation lookup failed.");
}

static async Task CorrelationIdIsUniqueAsync()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var repository = new EfJournalRepository(fixture.Context);
    var correlationId = Guid.NewGuid();

    await repository.AddAsync(CreateJournal(correlationId, 5000, 5000));

    var duplicateRejected = false;
    try
    {
        await repository.AddAsync(CreateJournal(correlationId, 7000, 7000));
    }
    catch (DbUpdateException)
    {
        duplicateRejected = true;
    }

    Assert(duplicateRejected, "Database accepted a duplicate correlation id.");
}

static async Task LineOrderIsPreservedAsync()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var repository = new EfJournalRepository(fixture.Context);
    var firstAccount = AccountId.New();
    var secondAccount = AccountId.New();
    var journal = JournalEntry.Create(
        JournalEntryId.New(),
        "EUR",
        "ORDER-TEST",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        [
            new LedgerLine(firstAccount, LedgerSide.Debit, 9900, "first"),
            new LedgerLine(secondAccount, LedgerSide.Credit, 9900, "second")
        ]);

    await repository.AddAsync(journal);
    var restored = await repository.GetByCorrelationIdAsync(journal.CorrelationId);

    Assert(restored is not null, "Journal was not restored.");
    Assert(restored!.Lines[0].AccountId == firstAccount, "First line order changed.");
    Assert(restored.Lines[1].AccountId == secondAccount, "Second line order changed.");
}

static JournalEntry CreateJournal(Guid correlationId, long debit, long credit) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        "xaf",
        "PERSISTENCE-SCENARIO",
        correlationId,
        DateTimeOffset.UtcNow,
        [
            new LedgerLine(AccountId.New(), LedgerSide.Debit, debit),
            new LedgerLine(AccountId.New(), LedgerSide.Credit, credit)
        ]);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class LedgerFixture : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    public LedgerDbContext Context { get; }

    private LedgerFixture(SqliteConnection connection, LedgerDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public static async Task<LedgerFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new LedgerDbContext(options);
        await context.Database.MigrateAsync();
        return new LedgerFixture(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
