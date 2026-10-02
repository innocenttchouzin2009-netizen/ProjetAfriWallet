using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.TransactionHistory.Application.Projection;
using AfriWallet.TransactionHistory.Infrastructure;
using AfriWallet.Wallet.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("reader returns only authorized wallet journals", AuthorizedWalletIsolation),
    ("reader projects debit and credit directions", DebitCreditDirections),
    ("reader paginates with stable timestamp and transaction cursor", StablePagination),
    ("unresolved authorized wallet fails closed", UnresolvedWalletFailsClosed),
    ("duplicate ledger account mapping fails closed", DuplicateMappingFailsClosed),
    ("journal shared by two authorized wallets produces one stable item", SharedAuthorizedJournalProducesSingleItem)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Transaction history infrastructure scenarios passed: {scenarios.Length}/{scenarios.Length}");

static async Task AuthorizedWalletIsolation()
{
    await using var fixture = await LedgerFixture.CreateAsync();

    var authorizedWallet = WalletId.New();
    var unauthorizedWallet = WalletId.New();
    var authorizedAccount = AccountId.New();
    var unauthorizedAccount = AccountId.New();
    var counterparty = AccountId.New();

    var authorizedTransactionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var unauthorizedTransactionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    fixture.AddJournal(
        authorizedTransactionId,
        new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero),
        "AUTH-001",
        "EUR",
        Line(authorizedAccount, LedgerSide.Debit, 1_000, 0),
        Line(counterparty, LedgerSide.Credit, 1_000, 1));

    fixture.AddJournal(
        unauthorizedTransactionId,
        new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero),
        "OTHER-001",
        "EUR",
        Line(unauthorizedAccount, LedgerSide.Debit, 2_000, 0),
        Line(counterparty, LedgerSide.Credit, 2_000, 1));

    await fixture.SaveAsync();

    var reader = fixture.Reader(new Dictionary<Guid, AccountId>
    {
        [authorizedWallet.Value] = authorizedAccount,
        [unauthorizedWallet.Value] = unauthorizedAccount
    });

    var page = await reader.ReadAsync(
        [authorizedWallet],
        TransactionHistoryPageRequest.Create());

    Assert(page.Items.Count == 1, "Only one authorized transaction is expected.");
    Assert(page.Items[0].TransactionId == authorizedTransactionId, "Unauthorized journal leaked into history.");
    Assert(page.Items[0].WalletId == authorizedWallet, "History item must preserve the authorized wallet id.");
}

static async Task DebitCreditDirections()
{
    await using var fixture = await LedgerFixture.CreateAsync();

    var debitWallet = WalletId.New();
    var creditWallet = WalletId.New();
    var debitAccount = AccountId.New();
    var creditAccount = AccountId.New();

    fixture.AddJournal(
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero),
        "P2P-001",
        "xaf",
        Line(debitAccount, LedgerSide.Debit, 12_500, 0),
        Line(creditAccount, LedgerSide.Credit, 12_500, 1));

    await fixture.SaveAsync();

    var mappings = new Dictionary<Guid, AccountId>
    {
        [debitWallet.Value] = debitAccount,
        [creditWallet.Value] = creditAccount
    };

    var debitPage = await fixture.Reader(mappings).ReadAsync(
        [debitWallet],
        TransactionHistoryPageRequest.Create());

    var creditPage = await fixture.Reader(mappings).ReadAsync(
        [creditWallet],
        TransactionHistoryPageRequest.Create());

    Assert(debitPage.Items.Single().Direction == TransactionHistoryDirection.Outgoing, "Debit must be outgoing.");
    Assert(creditPage.Items.Single().Direction == TransactionHistoryDirection.Incoming, "Credit must be incoming.");
    Assert(debitPage.Items.Single().CurrencyCode == "XAF", "Currency must be normalized by projector.");
    Assert(debitPage.Items.Single().Reference == "P2P-001", "Reference mismatch.");
    Assert(debitPage.Items.Single().Status == TransactionHistoryStatus.Completed, "Posted ledger entry must be completed.");
}

static async Task StablePagination()
{
    await using var fixture = await LedgerFixture.CreateAsync();

    var wallet = WalletId.New();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var sameTime = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero);

    var transactions = new[]
    {
        (Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), At: new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.Zero)),
        (Id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), At: sameTime),
        (Id: Guid.Parse("11111111-1111-1111-1111-111111111111"), At: sameTime),
        (Id: Guid.Parse("00000000-0000-0000-0000-000000000001"), At: new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero))
    };

    foreach (var transaction in transactions)
    {
        fixture.AddJournal(
            transaction.Id,
            transaction.At,
            $"TX-{transaction.Id:N}",
            "EUR",
            Line(account, LedgerSide.Debit, 100, 0),
            Line(counterparty, LedgerSide.Credit, 100, 1));
    }

    await fixture.SaveAsync();

    var reader = fixture.Reader(new Dictionary<Guid, AccountId> { [wallet.Value] = account });
    var expected = transactions
        .OrderByDescending(transaction => transaction.At)
        .ThenByDescending(transaction => transaction.Id)
        .Select(transaction => transaction.Id)
        .ToArray();

    var first = await reader.ReadAsync(
        [wallet],
        TransactionHistoryPageRequest.Create(limit: 2));

    Assert(first.Items.Select(item => item.TransactionId).SequenceEqual(expected.Take(2)), "First page ordering mismatch.");
    Assert(first.NextCursor is not null, "First page must provide a next cursor.");

    var second = await reader.ReadAsync(
        [wallet],
        TransactionHistoryPageRequest.Create(limit: 2, cursor: first.NextCursor));

    Assert(second.Items.Select(item => item.TransactionId).SequenceEqual(expected.Skip(2)), "Second page must continue without gaps or duplicates.");
    Assert(second.NextCursor is null, "Last page must not provide a next cursor.");
}

static async Task UnresolvedWalletFailsClosed()
{
    await using var fixture = await LedgerFixture.CreateAsync();

    var wallet = WalletId.New();
    var foreignAccount = AccountId.New();
    var counterparty = AccountId.New();

    fixture.AddJournal(
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero),
        "FOREIGN-001",
        "EUR",
        Line(foreignAccount, LedgerSide.Debit, 500, 0),
        Line(counterparty, LedgerSide.Credit, 500, 1));

    await fixture.SaveAsync();

    var page = await fixture.Reader(new Dictionary<Guid, AccountId>())
        .ReadAsync([wallet], TransactionHistoryPageRequest.Create());

    Assert(page.Items.Count == 0, "Unresolved wallet must not expose any ledger journal.");
    Assert(page.NextCursor is null, "Empty page must not expose a cursor.");
}

static async Task DuplicateMappingFailsClosed()
{
    await using var fixture = await LedgerFixture.CreateAsync();

    var walletA = WalletId.New();
    var walletB = WalletId.New();
    var sharedAccount = AccountId.New();

    var reader = fixture.Reader(new Dictionary<Guid, AccountId>
    {
        [walletA.Value] = sharedAccount,
        [walletB.Value] = sharedAccount
    });

    await AssertThrowsAsync<InvalidOperationException>(() =>
        reader.ReadAsync(
            [walletA, walletB],
            TransactionHistoryPageRequest.Create()));
}

static async Task SharedAuthorizedJournalProducesSingleItem()
{
    await using var fixture = await LedgerFixture.CreateAsync();

    var walletA = WalletId.New();
    var walletB = WalletId.New();
    var accountA = AccountId.New();
    var accountB = AccountId.New();
    var transactionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    fixture.AddJournal(
        transactionId,
        new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero),
        "INTERNAL-001",
        "EUR",
        Line(accountA, LedgerSide.Debit, 700, 0),
        Line(accountB, LedgerSide.Credit, 700, 1));

    await fixture.SaveAsync();

    var page = await fixture.Reader(new Dictionary<Guid, AccountId>
    {
        [walletA.Value] = accountA,
        [walletB.Value] = accountB
    }).ReadAsync(
        [walletA, walletB],
        TransactionHistoryPageRequest.Create());

    Assert(page.Items.Count == 1, "One ledger journal must produce one cursor-stable history item.");
    Assert(page.Items[0].TransactionId == transactionId, "Transaction id must be the ledger correlation id.");
    Assert(page.Items[0].WalletId == walletA, "Lowest ledger line position must deterministically select the represented wallet.");
}

static LedgerLineEntity Line(
    AccountId accountId,
    LedgerSide side,
    long amountMinor,
    int position) =>
    new()
    {
        Position = position,
        AccountId = accountId.Value,
        Side = (int)side,
        AmountMinor = amountMinor
    };

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task AssertThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

sealed class LedgerFixture : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private LedgerFixture(SqliteConnection connection, LedgerDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public LedgerDbContext Context { get; }

    public static async Task<LedgerFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LedgerDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new LedgerFixture(connection, context);
    }

    public void AddJournal(
        Guid correlationId,
        DateTimeOffset postedAtUtc,
        string businessReference,
        string currencyCode,
        params LedgerLineEntity[] lines)
    {
        var journal = new LedgerJournalEntity
        {
            Id = Guid.NewGuid(),
            CurrencyCode = currencyCode,
            BusinessReference = businessReference,
            CorrelationId = correlationId,
            PostedAtUtc = postedAtUtc,
            Lines = lines.ToList()
        };

        Context.JournalEntries.Add(journal);
    }

    public Task SaveAsync() => Context.SaveChangesAsync();

    public LedgerBackedTransactionHistoryReader Reader(
        IReadOnlyDictionary<Guid, AccountId> mappings) =>
        new(
            Context,
            new ConfiguredTransactionHistoryLedgerAccountResolver(mappings),
            new TransactionHistoryProjector());

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
