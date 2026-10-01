using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionRead.Application.Contracts;
using AfriWallet.TransactionRead.Application.Projection;
using AfriWallet.Wallet.Domain;

var walletId = WalletId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
var walletAccount = new AccountId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
var clearingAccount = new AccountId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
var now = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

var scenarios = new (string Name, Action Run)[]
{
    ("credit projects incoming", CreditProjectsIncoming),
    ("debit projects outgoing", DebitProjectsOutgoing),
    ("multiple wallet lines project net movement", MultipleWalletLinesProjectNetMovement),
    ("zero net wallet movement is omitted", ZeroNetMovementIsOmitted),
    ("foreign wallet account is omitted", ForeignWalletAccountIsOmitted),
    ("posted ledger fields remain authoritative", PostedLedgerFieldsRemainAuthoritative),
    ("request enforces page-size bounds", RequestEnforcesPageSizeBounds),
    ("cursor requires UTC and stable transaction id", CursorRequiresUtcAndStableTransactionId)
};

foreach (var scenario in scenarios)
{
    scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"Transaction read application scenarios passed: {scenarios.Length}/{scenarios.Length}");

void CreditProjectsIncoming()
{
    var journal = Journal(
        "XAF",
        "CREDIT-001",
        now.AddMinutes(-1),
        new LedgerLine(walletAccount, LedgerSide.Credit, 2_500),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 2_500));

    var item = LedgerTransactionProjector.Project(walletId, walletAccount, journal);

    Assert(item is not null, "Incoming movement must project an item.");
    Assert(item.Direction == TransactionReadDirection.Incoming,
        "Wallet credit must project as incoming.");
    Assert(item.AmountMinor == 2_500,
        "Incoming amount must remain in minor units.");
}

void DebitProjectsOutgoing()
{
    var journal = Journal(
        "EUR",
        "DEBIT-001",
        now.AddMinutes(-2),
        new LedgerLine(walletAccount, LedgerSide.Debit, 1_200),
        new LedgerLine(clearingAccount, LedgerSide.Credit, 1_200));

    var item = LedgerTransactionProjector.Project(walletId, walletAccount, journal);

    Assert(item is not null, "Outgoing movement must project an item.");
    Assert(item.Direction == TransactionReadDirection.Outgoing,
        "Wallet debit must project as outgoing.");
    Assert(item.AmountMinor == 1_200,
        "Outgoing amount must remain in minor units.");
}

void MultipleWalletLinesProjectNetMovement()
{
    var journal = Journal(
        "EUR",
        "NET-001",
        now.AddMinutes(-3),
        new LedgerLine(walletAccount, LedgerSide.Credit, 900),
        new LedgerLine(walletAccount, LedgerSide.Debit, 300),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 600));

    var item = LedgerTransactionProjector.Project(walletId, walletAccount, journal);

    Assert(item is not null, "Non-zero net movement must project.");
    Assert(item.Direction == TransactionReadDirection.Incoming,
        "Positive credit-minus-debit movement must be incoming.");
    Assert(item.AmountMinor == 600,
        "Projection must net all wallet lines from one journal.");
}

void ZeroNetMovementIsOmitted()
{
    var journal = Journal(
        "EUR",
        "ZERO-001",
        now.AddMinutes(-4),
        new LedgerLine(walletAccount, LedgerSide.Credit, 400),
        new LedgerLine(walletAccount, LedgerSide.Debit, 400),
        new LedgerLine(clearingAccount, LedgerSide.Credit, 100),
        new LedgerLine(
            new AccountId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            LedgerSide.Debit,
            100));

    var item = LedgerTransactionProjector.Project(walletId, walletAccount, journal);

    Assert(item is null,
        "Zero-net wallet movement must not fabricate a transaction direction.");
}

void ForeignWalletAccountIsOmitted()
{
    var foreignAccount = new AccountId(
        Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    var journal = Journal(
        "EUR",
        "FOREIGN-001",
        now.AddMinutes(-5),
        new LedgerLine(foreignAccount, LedgerSide.Credit, 700),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 700));

    var item = LedgerTransactionProjector.Project(walletId, walletAccount, journal);

    Assert(item is null,
        "A journal without the wallet ledger account must not be projected.");
}

void PostedLedgerFieldsRemainAuthoritative()
{
    var journal = Journal(
        "xaf",
        "  REF-001  ",
        now.AddMinutes(-6),
        new LedgerLine(walletAccount, LedgerSide.Credit, 350),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 350));

    var item = LedgerTransactionProjector.Project(walletId, walletAccount, journal);

    Assert(item is not null, "Ledger movement must project.");
    Assert(item.CurrencyCode == "XAF",
        "Ledger-normalized currency must be preserved.");
    Assert(item.Reference == "REF-001",
        "Ledger-normalized business reference must be preserved.");
    Assert(item.Status == TransactionReadStatus.Completed,
        "Posted ledger journal must project as completed.");
    Assert(item.OccurredAtUtc == now.AddMinutes(-6),
        "Posted ledger timestamp must remain authoritative.");
    Assert(item.CounterpartyLabel is null,
        "Internal ledger account identifiers must not leak as counterparties.");
}

void RequestEnforcesPageSizeBounds()
{
    var request = TransactionReadRequest.Create(walletId);

    Assert(request.Limit == TransactionReadRequest.DefaultLimit,
        "Default page size must be stable.");

    ExpectThrows<ArgumentOutOfRangeException>(
        () => TransactionReadRequest.Create(walletId, 0),
        "Zero page size must be rejected.");

    ExpectThrows<ArgumentOutOfRangeException>(
        () => TransactionReadRequest.Create(
            walletId,
            TransactionReadRequest.MaximumLimit + 1),
        "Oversized page must be rejected.");
}

void CursorRequiresUtcAndStableTransactionId()
{
    _ = new TransactionReadCursor(now, Guid.NewGuid());

    ExpectThrows<ArgumentException>(
        () => new TransactionReadCursor(
            now.ToOffset(TimeSpan.FromHours(1)),
            Guid.NewGuid()),
        "Non-UTC cursor timestamp must be rejected.");

    ExpectThrows<ArgumentException>(
        () => new TransactionReadCursor(now, Guid.Empty),
        "Empty cursor transaction id must be rejected.");
}

static JournalEntry Journal(
    string currencyCode,
    string reference,
    DateTimeOffset postedAtUtc,
    params LedgerLine[] lines) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        currencyCode,
        reference,
        Guid.NewGuid(),
        postedAtUtc,
        lines);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void ExpectThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}
