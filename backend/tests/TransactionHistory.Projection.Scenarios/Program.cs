using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Projection;
using AfriWallet.Wallet.Domain;

var scenarios = new (string Name, Action Run)[]
{
    ("credit projects incoming transaction", CreditProjectsIncoming),
    ("debit projects outgoing transaction", DebitProjectsOutgoing),
    ("currency and reference are normalized", CurrencyAndReferenceAreNormalized),
    ("status and counterparty are preserved", StatusAndCounterpartyArePreserved),
    ("projection orders newest transactions first", ProjectionOrdersNewestFirst),
    ("non-positive amount is rejected", NonPositiveAmountIsRejected),
    ("non-utc timestamp is rejected", NonUtcTimestampIsRejected)
};

foreach (var scenario in scenarios)
{
    scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Transaction history projection scenarios passed: {scenarios.Length}/{scenarios.Length}");

static void CreditProjectsIncoming()
{
    var projector = new TransactionHistoryProjector();
    var source = Source(side: LedgerSide.Credit, amountMinor: 12_500);

    var item = projector.Project(source);

    Assert(item.Direction == TransactionHistoryDirection.Incoming, "Credit must project as incoming.");
    Assert(item.AmountMinor == 12_500, "Amount mismatch.");
}

static void DebitProjectsOutgoing()
{
    var projector = new TransactionHistoryProjector();
    var source = Source(side: LedgerSide.Debit, amountMinor: 8_400);

    var item = projector.Project(source);

    Assert(item.Direction == TransactionHistoryDirection.Outgoing, "Debit must project as outgoing.");
    Assert(item.AmountMinor == 8_400, "Amount mismatch.");
}

static void CurrencyAndReferenceAreNormalized()
{
    var projector = new TransactionHistoryProjector();
    var source = Source(currencyCode: " xaf ", reference: "  P2P-123  ");

    var item = projector.Project(source);

    Assert(item.CurrencyCode == "XAF", "Currency must be normalized.");
    Assert(item.Reference == "P2P-123", "Reference must be trimmed.");
}

static void StatusAndCounterpartyArePreserved()
{
    var projector = new TransactionHistoryProjector();
    var source = Source(
        status: TransactionHistoryStatus.Reversed,
        counterpartyLabel: "@alice");

    var item = projector.Project(source);

    Assert(item.Status == TransactionHistoryStatus.Reversed, "Status mismatch.");
    Assert(item.CounterpartyLabel == "@alice", "Counterparty mismatch.");
}

static void ProjectionOrdersNewestFirst()
{
    var projector = new TransactionHistoryProjector();
    var older = Source(
        transactionId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        occurredAtUtc: new DateTimeOffset(2026, 10, 2, 7, 0, 0, TimeSpan.Zero));
    var newer = Source(
        transactionId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        occurredAtUtc: new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero));

    var items = projector.Project([older, newer]);

    Assert(items.Count == 2, "Expected two projected transactions.");
    Assert(items[0].TransactionId == newer.TransactionId, "Newest transaction must be first.");
    Assert(items[1].TransactionId == older.TransactionId, "Oldest transaction must be last.");
}

static void NonPositiveAmountIsRejected()
{
    var projector = new TransactionHistoryProjector();

    AssertThrows<ArgumentOutOfRangeException>(() => projector.Project(Source(amountMinor: 0)));
}

static void NonUtcTimestampIsRejected()
{
    var projector = new TransactionHistoryProjector();
    var localOffset = new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.FromHours(2));

    AssertThrows<ArgumentException>(() => projector.Project(Source(occurredAtUtc: localOffset)));
}

static TransactionHistoryProjectionSource Source(
    Guid? transactionId = null,
    long amountMinor = 1_000,
    string currencyCode = "EUR",
    LedgerSide side = LedgerSide.Credit,
    TransactionHistoryStatus status = TransactionHistoryStatus.Completed,
    DateTimeOffset? occurredAtUtc = null,
    string reference = "TX-001",
    string? counterpartyLabel = null) =>
    new(
        transactionId ?? Guid.NewGuid(),
        WalletId.New(),
        amountMinor,
        currencyCode,
        side,
        status,
        occurredAtUtc ?? new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero),
        reference,
        counterpartyLabel);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}
