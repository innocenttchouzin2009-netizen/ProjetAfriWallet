using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionRead.Application;
using AfriWallet.TransactionRead.Application.Abstractions;
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
    $"Transaction read projection scenarios passed: {scenarios.Length}/{scenarios.Length}");

var queryScenarios = new (string Name, Func<Task> Run)[]
{
    ("owned wallet query projects ledger page", OwnedWalletQueryProjectsLedgerPage),
    ("ownership denial short-circuits ledger read", OwnershipDenialShortCircuitsLedgerRead),
    ("full raw page preserves continuation across omitted projection", FullRawPagePreservesContinuation),
    ("short raw page terminates pagination", ShortRawPageTerminatesPagination),
    ("query rejects empty authenticated user", QueryRejectsEmptyAuthenticatedUser)
};

foreach (var scenario in queryScenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"Transaction read query scenarios passed: {queryScenarios.Length}/{queryScenarios.Length}");

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


async Task OwnedWalletQueryProjectsLedgerPage()
{
    var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    var first = Journal(
        "EUR",
        "PAGE-001",
        now.AddMinutes(-10),
        new LedgerLine(walletAccount, LedgerSide.Credit, 800),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 800));
    var second = Journal(
        "EUR",
        "PAGE-002",
        now.AddMinutes(-11),
        new LedgerLine(walletAccount, LedgerSide.Debit, 300),
        new LedgerLine(clearingAccount, LedgerSide.Credit, 300));

    var access = new RecordingWalletAccessReader(userId, walletId, walletAccount);
    var reader = new RecordingTransactionLedgerReader([first, second]);
    var service = new TransactionReadQueryService(reader, access);

    var page = await service.ReadAsync(
        userId,
        TransactionReadRequest.Create(walletId, 1));

    Assert(page.Items.Count == 1, "Query must honor requested item limit.");
    Assert(page.Items[0].TransactionId == first.Id.Value,
        "Query must preserve ledger reader ordering.");
    Assert(page.NextCursor is not null,
        "Overfetched projected item must produce a continuation cursor.");
    Assert(page.NextCursor.Value.TransactionId == first.Id.Value,
        "Continuation must resume strictly after the last returned item.");
    Assert(reader.LastTake == 2,
        "Query must overfetch exactly one raw journal for pagination.");
    Assert(reader.LastAccountId == walletAccount,
        "Ledger read must use the owned wallet ledger account.");
    Assert(access.Calls == 1, "Ownership must be resolved exactly once.");
}

async Task OwnershipDenialShortCircuitsLedgerRead()
{
    var userId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    var access = new RecordingWalletAccessReader(Guid.Empty, walletId, null);
    var reader = new RecordingTransactionLedgerReader([]);
    var service = new TransactionReadQueryService(reader, access);

    await ExpectThrowsAsync<UnauthorizedAccessException>(
        () => service.ReadAsync(
            userId,
            TransactionReadRequest.Create(walletId)),
        "Unowned wallet must be rejected.");

    Assert(reader.Calls == 0,
        "Ledger must not be queried when wallet ownership fails.");
}

async Task FullRawPagePreservesContinuation()
{
    var userId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    var visible = Journal(
        "EUR",
        "VISIBLE-001",
        now.AddMinutes(-20),
        new LedgerLine(walletAccount, LedgerSide.Credit, 500),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 500));
    var omitted = Journal(
        "EUR",
        "OMITTED-001",
        now.AddMinutes(-21),
        new LedgerLine(walletAccount, LedgerSide.Credit, 400),
        new LedgerLine(walletAccount, LedgerSide.Debit, 400),
        new LedgerLine(clearingAccount, LedgerSide.Credit, 100),
        new LedgerLine(
            new AccountId(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
            LedgerSide.Debit,
            100));

    var access = new RecordingWalletAccessReader(userId, walletId, walletAccount);
    var reader = new RecordingTransactionLedgerReader([visible, omitted]);
    var service = new TransactionReadQueryService(reader, access);

    var page = await service.ReadAsync(
        userId,
        TransactionReadRequest.Create(walletId, 1));

    Assert(page.Items.Count == 1,
        "Zero-net journal must remain omitted by projection.");
    Assert(page.NextCursor is not null,
        "A full raw page must preserve continuation even when projection omits a journal.");
    Assert(page.NextCursor.Value.TransactionId == omitted.Id.Value,
        "Continuation must advance past the last consumed raw journal.");
}

async Task ShortRawPageTerminatesPagination()
{
    var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    var journal = Journal(
        "EUR",
        "LAST-001",
        now.AddMinutes(-30),
        new LedgerLine(walletAccount, LedgerSide.Credit, 250),
        new LedgerLine(clearingAccount, LedgerSide.Debit, 250));

    var access = new RecordingWalletAccessReader(userId, walletId, walletAccount);
    var reader = new RecordingTransactionLedgerReader([journal]);
    var service = new TransactionReadQueryService(reader, access);

    var page = await service.ReadAsync(
        userId,
        TransactionReadRequest.Create(walletId, 2));

    Assert(page.Items.Count == 1, "Available transaction must be returned.");
    Assert(page.NextCursor is null,
        "Short raw page must signal end of pagination.");
}

async Task QueryRejectsEmptyAuthenticatedUser()
{
    var access = new RecordingWalletAccessReader(Guid.Empty, walletId, walletAccount);
    var reader = new RecordingTransactionLedgerReader([]);
    var service = new TransactionReadQueryService(reader, access);

    await ExpectThrowsAsync<ArgumentException>(
        () => service.ReadAsync(
            Guid.Empty,
            TransactionReadRequest.Create(walletId)),
        "Empty authenticated user id must be rejected.");

    Assert(access.Calls == 0,
        "Ownership must not be queried for an invalid authenticated user.");
    Assert(reader.Calls == 0,
        "Ledger must not be queried for an invalid authenticated user.");
}

static async Task ExpectThrowsAsync<TException>(
    Func<Task> action,
    string message)
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

    throw new InvalidOperationException(message);
}

sealed class RecordingWalletAccessReader(
    Guid ownedByUserId,
    WalletId ownedWalletId,
    AccountId? accountId) : ITransactionReadWalletAccessReader
{
    public int Calls { get; private set; }

    public Task<AccountId?> ResolveOwnedLedgerAccountAsync(
        Guid authenticatedUserId,
        WalletId candidateWalletId,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        cancellationToken.ThrowIfCancellationRequested();

        var owned = authenticatedUserId == ownedByUserId
            && candidateWalletId == ownedWalletId;

        return Task.FromResult(owned ? accountId : null);
    }
}

sealed class RecordingTransactionLedgerReader(
    IReadOnlyList<JournalEntry> journals) : ITransactionLedgerReader
{
    public int Calls { get; private set; }
    public AccountId? LastAccountId { get; private set; }
    public TransactionReadCursor? LastCursor { get; private set; }
    public int LastTake { get; private set; }

    public Task<IReadOnlyList<JournalEntry>> ReadAsync(
        AccountId walletAccountId,
        TransactionReadCursor? cursor,
        int take,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastAccountId = walletAccountId;
        LastCursor = cursor;
        LastTake = take;
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(journals);
    }
}
