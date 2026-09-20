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
    try { action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}

var ownerId = Guid.NewGuid();
var walletId = WalletId.From(Guid.NewGuid());
var source = FinancialActivitySource.Create("  ledger  ", "journal-001");
var occurredAt = new DateTimeOffset(2026, 9, 20, 17, 0, 0, TimeSpan.Zero);

var activity = FinancialActivity.Create(
    ownerId,
    walletId,
    source,
    FinancialActivityKind.LedgerPosting,
    FinancialActivityDirection.Outgoing,
    FinancialActivityState.Completed,
    Currency.Create(" xaf "),
    12_500,
    occurredAt,
    occurredAt.AddMinutes(1),
    "  Paiement marchand  ");

Assert(activity.Source.System == "ledger", "Source system must normalize.");
Assert(activity.Source.Key == "ledger:journal-001", "Source key must be stable.");
Assert(activity.Currency.Code == "XAF", "Currency normalization must be preserved.");
Assert(activity.AmountMinor == 12_500, "Amount must be preserved.");
Assert(activity.Summary == "Paiement marchand", "Summary must trim.");

var query = FinancialActivityQuery.Create(ownerId, walletId);
Assert(query.OwnerId == ownerId && query.WalletId == walletId, "Query scope must be preserved.");

var result = FinancialActivityResult.Create([activity]);
Assert(result.Items.Count == 1 && result.Items[0] == activity, "Result must preserve activities.");

AssertThrows<ArgumentException>(
    () => FinancialActivitySource.Create("ledger system", "id"),
    "Source system whitespace must be rejected.");
AssertThrows<ArgumentException>(
    () => FinancialActivitySource.Create("ledger", " padded "),
    "Source id surrounding whitespace must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => FinancialActivity.Create(ownerId, walletId, source, FinancialActivityKind.LedgerPosting,
        FinancialActivityDirection.Outgoing, FinancialActivityState.Completed, Currency.Create("XAF"),
        0, occurredAt, occurredAt),
    "Non-positive amount must be rejected.");
AssertThrows<ArgumentException>(
    () => FinancialActivity.Create(ownerId, walletId, source, FinancialActivityKind.LedgerPosting,
        FinancialActivityDirection.Outgoing, FinancialActivityState.Completed, Currency.Create("XAF"),
        100, occurredAt, occurredAt.AddMinutes(-1)),
    "Backward update timestamp must be rejected.");
AssertThrows<ArgumentException>(
    () => FinancialActivityQuery.Create(Guid.Empty),
    "Empty owner id must be rejected.");

Console.WriteLine("AFW-BE-TIMELINE-1 financial activity domain and query contract scenarios: PASS");
