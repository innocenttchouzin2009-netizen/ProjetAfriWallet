using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("adapter reads matching account and currency journals", ReadsMatchingAccountAndCurrency),
    ("adapter excludes unrelated account and currency journals", ExcludesUnrelatedJournals),
    ("adapter reconstructs journals in deterministic order", ReconstructsInDeterministicOrder),
    ("adapter is read-only and does not track entities", AdapterIsReadOnly),
    ("ledger-backed balance service projects through concrete adapter", ProjectsThroughConcreteAdapter)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Balance infrastructure scenarios passed: {scenarios.Length}/{scenarios.Length}");

static async Task ReadsMatchingAccountAndCurrency()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var expected = Journal("XAF", account, LedgerSide.Credit, counterparty, LedgerSide.Debit, 5_000, DateTimeOffset.UtcNow.AddMinutes(-2));
    await fixture.Repository.AddAsync(expected);

    var journals = await fixture.Reader.ReadAsync(new BalanceKey(account, "xaf"));

    Assert(journals.Count == 1, "Expected one matching journal.");
    Assert(journals[0].Id == expected.Id, "Unexpected journal returned.");
}

static async Task ExcludesUnrelatedJournals()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var account = AccountId.New();
    var other = AccountId.New();
    var counterparty = AccountId.New();

    await fixture.Repository.AddAsync(Journal("XAF", account, LedgerSide.Credit, counterparty, LedgerSide.Debit, 1_000, DateTimeOffset.UtcNow.AddMinutes(-3)));
    await fixture.Repository.AddAsync(Journal("EUR", account, LedgerSide.Credit, counterparty, LedgerSide.Debit, 2_000, DateTimeOffset.UtcNow.AddMinutes(-2)));
    await fixture.Repository.AddAsync(Journal("XAF", other, LedgerSide.Credit, counterparty, LedgerSide.Debit, 3_000, DateTimeOffset.UtcNow.AddMinutes(-1)));

    var journals = await fixture.Reader.ReadAsync(new BalanceKey(account, "XAF"));

    Assert(journals.Count == 1, "Adapter must exclude unrelated account/currency journals.");
    Assert(journals[0].CurrencyCode == "XAF", "Wrong currency leaked through adapter.");
}

static async Task ReconstructsInDeterministicOrder()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var earlier = Journal("USD", account, LedgerSide.Debit, counterparty, LedgerSide.Credit, 700, DateTimeOffset.UtcNow.AddMinutes(-5));
    var later = Journal("USD", account, LedgerSide.Credit, counterparty, LedgerSide.Debit, 300, DateTimeOffset.UtcNow.AddMinutes(-1));
    await fixture.Repository.AddAsync(later);
    await fixture.Repository.AddAsync(earlier);

    var journals = await fixture.Reader.ReadAsync(new BalanceKey(account, "USD"));

    Assert(journals.Count == 2, "Expected two journals.");
    Assert(journals[0].Id == earlier.Id && journals[1].Id == later.Id, "Journal order must be deterministic by posted time then id.");
    Assert(journals[0].Lines[0].AccountId == account, "Ledger line order was not preserved.");
}

static async Task AdapterIsReadOnly()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    await fixture.Repository.AddAsync(Journal("XAF", account, LedgerSide.Credit, counterparty, LedgerSide.Debit, 1_500, DateTimeOffset.UtcNow));
    fixture.Context.ChangeTracker.Clear();

    _ = await fixture.Reader.ReadAsync(new BalanceKey(account, "XAF"));

    Assert(!fixture.Context.ChangeTracker.Entries().Any(), "Adapter must use no-tracking reads.");
}

static async Task ProjectsThroughConcreteAdapter()
{
    await using var fixture = await LedgerFixture.CreateAsync();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    await fixture.Repository.AddAsync(Journal("EUR", account, LedgerSide.Credit, counterparty, LedgerSide.Debit, 8_000, DateTimeOffset.UtcNow.AddMinutes(-2)));
    await fixture.Repository.AddAsync(Journal("EUR", account, LedgerSide.Debit, counterparty, LedgerSide.Credit, 2_500, DateTimeOffset.UtcNow.AddMinutes(-1)));

    var service = new LedgerBackedBalanceReadService(fixture.Reader, new BalanceProjectionService());
    var snapshot = await service.ReadAsync(new BalanceKey(account, "EUR"));

    Assert(snapshot.DebitMinor == 2_500, "Debit projection mismatch.");
    Assert(snapshot.CreditMinor == 8_000, "Credit projection mismatch.");
    Assert(snapshot.NetMinor == 5_500, "Net projection mismatch.");
}

static JournalEntry Journal(
    string currency,
    AccountId account,
    LedgerSide accountSide,
    AccountId counterparty,
    LedgerSide counterpartySide,
    long amountMinor,
    DateTimeOffset postedAtUtc) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        currency,
        $"BAL-INFRA-{Guid.NewGuid():N}",
        Guid.NewGuid(),
        postedAtUtc,
        [
            new LedgerLine(account, accountSide, amountMinor),
            new LedgerLine(counterparty, counterpartySide, amountMinor)
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

    private LedgerFixture(SqliteConnection connection, LedgerDbContext context)
    {
        this.connection = connection;
        Context = context;
        Repository = new EfJournalRepository(context);
        Reader = new EfLedgerJournalReader(context);
    }

    public LedgerDbContext Context { get; }
    public IJournalRepository Repository { get; }
    public ILedgerJournalReader Reader { get; }

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
