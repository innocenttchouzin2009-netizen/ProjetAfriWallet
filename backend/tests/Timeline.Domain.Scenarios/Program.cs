using AfriWallet.Timeline.Application;
using AfriWallet.Timeline.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message)
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

var ownerId = Guid.NewGuid();
var walletId = WalletId.From(Guid.NewGuid());
var occurredAt = new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
var source = TimelineSourceReference.Create("P2P", Guid.NewGuid().ToString("N"));
var entry = FinancialTimelineEntry.Create(
    ownerId,
    walletId,
    source,
    FinancialTimelineEntryKind.MoneyTransfer,
    FinancialTimelineDirection.Outgoing,
    FinancialTimelineState.Completed,
    Currency.Create(" xaf "),
    25_000,
    occurredAt,
    occurredAt.AddSeconds(2),
    "Payment sent");

Assert(entry.OwnerId == ownerId, "Owner must be preserved.");
Assert(entry.WalletId == walletId, "Wallet must be preserved.");
Assert(entry.Source.SourceSystem == "p2p", "Source system must be normalized.");
Assert(entry.Currency.Code == "XAF", "Currency normalization must be preserved.");
Assert(entry.AmountMinor == 25_000, "Amount must be preserved.");
Assert(entry.Summary == "Payment sent", "Summary must be normalized and preserved.");
Assert(entry.Direction == FinancialTimelineDirection.Outgoing, "Direction must be preserved.");
Assert(entry.State == FinancialTimelineState.Completed, "State must be preserved.");

var query = FinancialTimelineQuery.Create(ownerId, walletId, 25, "  opaque-cursor  ");
Assert(query.OwnerId == ownerId, "Query owner must be preserved.");
Assert(query.WalletId == walletId, "Wallet filter must be preserved.");
Assert(query.Limit == 25, "Limit must be preserved.");
Assert(query.Cursor == "opaque-cursor", "Cursor must be normalized.");

var page = FinancialTimelinePage.Create([entry], " next-page ");
Assert(page.Items.Count == 1, "Page must preserve items.");
Assert(page.NextCursor == "next-page", "Next cursor must be normalized.");

AssertThrows<ArgumentException>(
    () => TimelineSourceReference.Create("p2p source", "1"),
    "Source system whitespace must be rejected.");
AssertThrows<ArgumentException>(
    () => TimelineSourceReference.Create("p2p", " source-id "),
    "Opaque source id surrounding whitespace must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => FinancialTimelineEntry.Create(ownerId, walletId, source, FinancialTimelineEntryKind.MoneyTransfer,
        FinancialTimelineDirection.Outgoing, FinancialTimelineState.Completed, Currency.Create("EUR"), 0,
        occurredAt, occurredAt),
    "Zero amount must be rejected.");
AssertThrows<ArgumentException>(
    () => FinancialTimelineEntry.Create(ownerId, walletId, source, FinancialTimelineEntryKind.PaymentRequest,
        FinancialTimelineDirection.Incoming, FinancialTimelineState.Pending, Currency.Create("EUR"), 100,
        occurredAt, occurredAt.AddMinutes(-1)),
    "Updated timestamp cannot precede occurrence timestamp.");
AssertThrows<ArgumentException>(
    () => FinancialTimelineEntry.Create(ownerId, walletId, source, FinancialTimelineEntryKind.PaymentRequest,
        FinancialTimelineDirection.Incoming, FinancialTimelineState.Pending, Currency.Create("EUR"), 100,
        occurredAt.ToOffset(TimeSpan.FromHours(2)), occurredAt),
    "Non-UTC occurrence timestamp must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => FinancialTimelineQuery.Create(ownerId, limit: 0),
    "Limit below range must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => FinancialTimelineQuery.Create(ownerId, limit: 101),
    "Limit above range must be rejected.");
AssertThrows<ArgumentException>(
    () => FinancialTimelineQuery.Create(Guid.Empty),
    "Empty owner id must be rejected.");

var reader = new RecordingReader(page);
var returned = await reader.ReadAsync(query);
Assert(returned == page, "Reader contract must return a timeline page.");
Assert(reader.LastQuery == query, "Reader contract must receive the query unchanged.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await reader.ReadAsync(query, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-TIMELINE-1 domain and read-model contract scenarios: PASS");

sealed class RecordingReader(FinancialTimelinePage page) : IFinancialTimelineReader
{
    public FinancialTimelineQuery? LastQuery { get; private set; }

    public Task<FinancialTimelinePage> ReadAsync(
        FinancialTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastQuery = query;
        return Task.FromResult(page);
    }
}
