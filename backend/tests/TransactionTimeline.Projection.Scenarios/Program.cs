using AfriWallet.TransactionTimeline.Application.Contracts;
using AfriWallet.TransactionTimeline.Application.Projection;

var walletA = Guid.Parse("11111111-1111-1111-1111-111111111111");
var walletB = Guid.Parse("22222222-2222-2222-2222-222222222222");
var service = new TransactionTimelineProjectionService();
var now = new DateTimeOffset(2026, 9, 27, 8, 0, 0, TimeSpan.Zero);

var scenarios = new (string Name, Action Run)[]
{
    ("projection filters by wallet", ProjectionFiltersByWallet),
    ("projection orders newest first", ProjectionOrdersNewestFirst),
    ("before cursor is exclusive", BeforeCursorIsExclusive),
    ("page limit exposes continuation", PageLimitExposesContinuation),
    ("projection normalizes read fields", ProjectionNormalizesReadFields),
    ("invalid limit is rejected", InvalidLimitIsRejected)
};

foreach (var scenario in scenarios)
{
    scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Transaction timeline projection scenarios passed: {scenarios.Length}/{scenarios.Length}");

void ProjectionFiltersByWallet()
{
    var page = service.Project(
        new TransactionTimelineReadRequest(walletA),
        new[]
        {
            Entry(walletA, "tx-a", now),
            Entry(walletB, "tx-b", now.AddMinutes(1))
        });

    Assert(page.Items.Count == 1, "Only the requested wallet must be projected.");
    Assert(page.Items[0].TransactionId == "tx-a", "Unexpected projected transaction.");
}

void ProjectionOrdersNewestFirst()
{
    var page = service.Project(
        new TransactionTimelineReadRequest(walletA),
        new[]
        {
            Entry(walletA, "older", now.AddMinutes(-2)),
            Entry(walletA, "newer", now)
        });

    Assert(page.Items.Select(x => x.TransactionId).SequenceEqual(new[] { "newer", "older" }),
        "Timeline must be ordered newest first.");
}

void BeforeCursorIsExclusive()
{
    var page = service.Project(
        new TransactionTimelineReadRequest(walletA, Before: now),
        new[]
        {
            Entry(walletA, "at-cursor", now),
            Entry(walletA, "before-cursor", now.AddSeconds(-1))
        });

    Assert(page.Items.Count == 1 && page.Items[0].TransactionId == "before-cursor",
        "Before cursor must exclude entries at the cursor instant.");
}

void PageLimitExposesContinuation()
{
    var page = service.Project(
        new TransactionTimelineReadRequest(walletA, Limit: 2),
        new[]
        {
            Entry(walletA, "tx-3", now),
            Entry(walletA, "tx-2", now.AddMinutes(-1)),
            Entry(walletA, "tx-1", now.AddMinutes(-2))
        });

    Assert(page.Items.Count == 2, "Page size must honor the requested limit.");
    Assert(page.HasMore, "A hidden extra item must expose continuation.");
    Assert(page.NextBefore == page.Items[^1].OccurredAt, "Next cursor must use the last visible timestamp.");
}

void ProjectionNormalizesReadFields()
{
    var page = service.Project(
        new TransactionTimelineReadRequest(walletA),
        new[]
        {
            new TransactionTimelineProjectionEntry(
                walletA,
                " tx-1 ",
                125000,
                "xaf",
                TransactionTimelineDirection.Incoming,
                TransactionTimelineStatus.Completed,
                now.ToOffset(TimeSpan.FromHours(2)),
                " ref-1 ",
                " Alice ")
        });

    var item = page.Items.Single();
    Assert(item.TransactionId == "tx-1", "TransactionId must be trimmed.");
    Assert(item.CurrencyCode == "XAF", "Currency must be normalized.");
    Assert(item.Reference == "ref-1", "Reference must be trimmed.");
    Assert(item.CounterpartyLabel == "Alice", "Counterparty label must be trimmed.");
    Assert(item.OccurredAt.Offset == TimeSpan.Zero, "Timeline timestamps must be projected in UTC.");
}

void InvalidLimitIsRejected()
{
    try
    {
        service.Project(new TransactionTimelineReadRequest(walletA, Limit: 0), Array.Empty<TransactionTimelineProjectionEntry>());
        throw new InvalidOperationException("Invalid limit was not rejected.");
    }
    catch (ArgumentOutOfRangeException)
    {
    }
}

TransactionTimelineProjectionEntry Entry(Guid walletId, string id, DateTimeOffset occurredAt) =>
    new(
        walletId,
        id,
        100,
        "EUR",
        TransactionTimelineDirection.Outgoing,
        TransactionTimelineStatus.Completed,
        occurredAt,
        $"ref-{id}",
        null);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
