using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.TransactionTimeline.Application.Contracts;
using AfriWallet.TransactionTimeline.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var walletA = Guid.Parse("11111111-1111-1111-1111-111111111111");
var walletB = Guid.Parse("22222222-2222-2222-2222-222222222222");
var accountA = new AccountId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
var accountB = new AccountId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
var clearing = new AccountId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
var now = new DateTimeOffset(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<LedgerDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new LedgerDbContext(options);
await db.Database.EnsureCreatedAsync();

var incomingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
var outgoingId = Guid.Parse("10000000-0000-0000-0000-000000000002");
var otherWalletId = Guid.Parse("10000000-0000-0000-0000-000000000003");
var nettedId = Guid.Parse("10000000-0000-0000-0000-000000000004");
var zeroNetId = Guid.Parse("10000000-0000-0000-0000-000000000005");

db.JournalEntries.AddRange(
    Journal(
        incomingId,
        "xaf",
        " CREDIT-001 ",
        now.AddMinutes(-4),
        Line(incomingId, 1, accountA, LedgerSide.Credit, 2_500),
        Line(incomingId, 2, clearing, LedgerSide.Debit, 2_500)),
    Journal(
        outgoingId,
        "EUR",
        "DEBIT-001",
        now.AddMinutes(-3),
        Line(outgoingId, 1, accountA, LedgerSide.Debit, 1_200),
        Line(outgoingId, 2, clearing, LedgerSide.Credit, 1_200)),
    Journal(
        otherWalletId,
        "EUR",
        "OTHER-001",
        now.AddMinutes(-2),
        Line(otherWalletId, 1, accountB, LedgerSide.Credit, 999),
        Line(otherWalletId, 2, clearing, LedgerSide.Debit, 999)),
    Journal(
        nettedId,
        "EUR",
        "NET-001",
        now.AddMinutes(-1),
        Line(nettedId, 1, accountA, LedgerSide.Credit, 900),
        Line(nettedId, 2, accountA, LedgerSide.Debit, 300),
        Line(nettedId, 3, clearing, LedgerSide.Debit, 600)),
    Journal(
        zeroNetId,
        "EUR",
        "ZERO-001",
        now,
        Line(zeroNetId, 1, accountA, LedgerSide.Credit, 400),
        Line(zeroNetId, 2, accountA, LedgerSide.Debit, 400),
        Line(zeroNetId, 3, clearing, LedgerSide.Credit, 100),
        Line(zeroNetId, 4, new AccountId(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")), LedgerSide.Debit, 100)));

await db.SaveChangesAsync();

var mappings = new Dictionary<Guid, AccountId>
{
    [walletA] = accountA,
    [walletB] = accountB
};

var source = new LedgerTransactionTimelineSource(db, mappings);

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("ledger projection filters by wallet account", FiltersByWalletAccount),
    ("credit projects incoming and debit projects outgoing", ProjectsDirection),
    ("ledger projection uses posted journal fields", ProjectsStableFields),
    ("multiple wallet lines project one net movement", ProjectsNetMovement),
    ("zero net wallet movement is omitted", OmitsZeroNetMovement),
    ("before cursor is exclusive", BeforeCursorIsExclusive),
    ("limit exposes continuation newest first", LimitExposesContinuation),
    ("read does not mutate ledger journal", ReadDoesNotMutateLedger)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"Transaction timeline ledger projection scenarios passed: {scenarios.Length}/{scenarios.Length}");

async Task FiltersByWalletAccount()
{
    var page = await source.ReadAsync(new TransactionTimelineReadRequest(walletA));

    Assert(page.Items.All(item => item.Reference != "OTHER-001"),
        "A foreign wallet journal must not be projected.");
}

async Task ProjectsDirection()
{
    var page = await source.ReadAsync(new TransactionTimelineReadRequest(walletA));

    var incoming = page.Items.Single(item => item.TransactionId == incomingId.ToString("D"));
    var outgoing = page.Items.Single(item => item.TransactionId == outgoingId.ToString("D"));

    Assert(incoming.Direction == TransactionTimelineDirection.Incoming,
        "Ledger credit must project as incoming.");
    Assert(outgoing.Direction == TransactionTimelineDirection.Outgoing,
        "Ledger debit must project as outgoing.");
}

async Task ProjectsStableFields()
{
    var page = await source.ReadAsync(new TransactionTimelineReadRequest(walletA));
    var item = page.Items.Single(entry => entry.TransactionId == incomingId.ToString("D"));

    Assert(item.AmountMinor == 2_500, "Ledger amount must be preserved in minor units.");
    Assert(item.CurrencyCode == "XAF", "Currency must be normalized for the read model.");
    Assert(item.Status == TransactionTimelineStatus.Completed,
        "A posted ledger journal must project as completed.");
    Assert(item.Reference == "CREDIT-001", "Business reference must be trimmed.");
    Assert(item.OccurredAt == now.AddMinutes(-4),
        "Ledger posted timestamp must be preserved.");
    Assert(item.CounterpartyLabel is null,
        "Internal ledger account ids must not be exposed as counterparty labels.");
}

async Task ProjectsNetMovement()
{
    var page = await source.ReadAsync(new TransactionTimelineReadRequest(walletA));
    var item = page.Items.Single(entry => entry.TransactionId == nettedId.ToString("D"));

    Assert(item.Direction == TransactionTimelineDirection.Incoming,
        "Positive credit-minus-debit movement must project as incoming.");
    Assert(item.AmountMinor == 600,
        "Multiple wallet lines must be projected as one net journal movement.");
}

async Task OmitsZeroNetMovement()
{
    var page = await source.ReadAsync(new TransactionTimelineReadRequest(walletA));

    Assert(page.Items.All(item => item.TransactionId != zeroNetId.ToString("D")),
        "A zero-net wallet movement must not fabricate a direction.");
}

async Task BeforeCursorIsExclusive()
{
    var page = await source.ReadAsync(
        new TransactionTimelineReadRequest(walletA, Before: now.AddMinutes(-1)));

    Assert(page.Items.All(item => item.OccurredAt < now.AddMinutes(-1)),
        "Before cursor must be exclusive.");
}

async Task LimitExposesContinuation()
{
    var page = await source.ReadAsync(
        new TransactionTimelineReadRequest(walletA, Limit: 2));

    Assert(page.Items.Count == 2, "Timeline limit must be enforced.");
    Assert(page.HasMore, "An additional visible ledger movement must expose continuation.");
    Assert(page.Items[0].OccurredAt >= page.Items[1].OccurredAt,
        "Timeline must be ordered newest first.");
    Assert(page.NextBefore == page.Items[^1].OccurredAt,
        "Continuation cursor must use the last visible timestamp.");
}

async Task ReadDoesNotMutateLedger()
{
    var beforeEntries = await db.JournalEntries.AsNoTracking().CountAsync();
    var beforeLines = await db.Lines.AsNoTracking().CountAsync();

    _ = await source.ReadAsync(new TransactionTimelineReadRequest(walletA));

    var afterEntries = await db.JournalEntries.AsNoTracking().CountAsync();
    var afterLines = await db.Lines.AsNoTracking().CountAsync();

    Assert(beforeEntries == afterEntries, "Timeline reads must not mutate journal entries.");
    Assert(beforeLines == afterLines, "Timeline reads must not mutate ledger lines.");
    Assert(!db.ChangeTracker.HasChanges(), "Timeline reads must leave no tracked mutations.");
}

static LedgerJournalEntity Journal(
    Guid id,
    string currency,
    string reference,
    DateTimeOffset postedAtUtc,
    params LedgerLineEntity[] lines) =>
    new()
    {
        Id = id,
        CurrencyCode = currency,
        BusinessReference = reference,
        CorrelationId = Guid.NewGuid(),
        PostedAtUtc = postedAtUtc,
        Lines = lines.ToList()
    };

static LedgerLineEntity Line(
    Guid journalId,
    int position,
    AccountId accountId,
    LedgerSide side,
    long amountMinor) =>
    new()
    {
        JournalEntryId = journalId,
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
