using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("normalized balance key is passed to ledger reader", NormalizedKeyIsPassedToReader),
    ("ledger journals are projected into current balance", LedgerJournalsAreProjected),
    ("empty ledger read returns zero balance", EmptyLedgerReadReturnsZero),
    ("unrelated ledger entries are ignored defensively", UnrelatedEntriesAreIgnored),
    ("cancellation token is propagated to ledger reader", CancellationTokenIsPropagated),
    ("repeated reads do not retain mutable balance state", RepeatedReadsDoNotRetainState)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Balance read scenarios passed: {scenarios.Length}/{scenarios.Length}");

static async Task NormalizedKeyIsPassedToReader()
{
    var account = AccountId.New();
    var reader = new RecordingLedgerJournalReader([]);
    var service = Service(reader);

    await service.ReadAsync(new BalanceKey(account, " xaf "));

    var key = reader.LastKey;
    Assert(key.HasValue, "Expected ledger reader to receive a balance key.");
    Assert(key.Value.AccountId == account, "Ledger reader received the wrong account.");
    Assert(key.Value.CurrencyCode == "XAF", "Ledger reader must receive the normalized currency.");
}

static async Task LedgerJournalsAreProjected()
{
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var journals = new[]
    {
        Journal("EUR", new LedgerLine(account, LedgerSide.Credit, 8_000), new LedgerLine(counterparty, LedgerSide.Debit, 8_000)),
        Journal("EUR", new LedgerLine(account, LedgerSide.Debit, 2_500), new LedgerLine(counterparty, LedgerSide.Credit, 2_500))
    };
    var reader = new RecordingLedgerJournalReader(journals);
    var service = Service(reader);

    var snapshot = await service.ReadAsync(new BalanceKey(account, "EUR"));

    Assert(snapshot.DebitMinor == 2_500, "Debit projection mismatch.");
    Assert(snapshot.CreditMinor == 8_000, "Credit projection mismatch.");
    Assert(snapshot.NetMinor == 5_500, "Net projection mismatch.");
}

static async Task EmptyLedgerReadReturnsZero()
{
    var key = new BalanceKey(AccountId.New(), "USD");
    var service = Service(new RecordingLedgerJournalReader([]));

    var snapshot = await service.ReadAsync(key);

    Assert(snapshot.Key == key, "Zero snapshot key mismatch.");
    Assert(snapshot.DebitMinor == 0 && snapshot.CreditMinor == 0 && snapshot.NetMinor == 0, "Expected zero balance.");
}

static async Task UnrelatedEntriesAreIgnored()
{
    var account = AccountId.New();
    var otherAccount = AccountId.New();
    var counterparty = AccountId.New();
    var journals = new[]
    {
        Journal("XAF", new LedgerLine(account, LedgerSide.Credit, 4_000), new LedgerLine(counterparty, LedgerSide.Debit, 4_000)),
        Journal("EUR", new LedgerLine(account, LedgerSide.Credit, 9_000), new LedgerLine(counterparty, LedgerSide.Debit, 9_000)),
        Journal("XAF", new LedgerLine(otherAccount, LedgerSide.Credit, 7_000), new LedgerLine(counterparty, LedgerSide.Debit, 7_000))
    };
    var service = Service(new RecordingLedgerJournalReader(journals));

    var snapshot = await service.ReadAsync(new BalanceKey(account, "XAF"));

    Assert(snapshot.CreditMinor == 4_000, "Projection must ignore unrelated account/currency entries.");
    Assert(snapshot.NetMinor == 4_000, "Unexpected unrelated ledger contribution.");
}

static async Task CancellationTokenIsPropagated()
{
    var reader = new RecordingLedgerJournalReader([]);
    var service = Service(reader);
    using var cancellation = new CancellationTokenSource();

    await service.ReadAsync(new BalanceKey(AccountId.New(), "XAF"), cancellation.Token);

    Assert(reader.LastCancellationToken == cancellation.Token, "Cancellation token was not propagated.");
}

static async Task RepeatedReadsDoNotRetainState()
{
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var first = Journal("XAF", new LedgerLine(account, LedgerSide.Credit, 100), new LedgerLine(counterparty, LedgerSide.Debit, 100));
    var second = Journal("XAF", new LedgerLine(account, LedgerSide.Credit, 50), new LedgerLine(counterparty, LedgerSide.Debit, 50));
    var reader = new SequencedLedgerJournalReader([[first], [first, second]]);
    var service = Service(reader);
    var key = new BalanceKey(account, "XAF");

    var firstRead = await service.ReadAsync(key);
    var secondRead = await service.ReadAsync(key);

    Assert(firstRead.NetMinor == 100, "First read mismatch.");
    Assert(secondRead.NetMinor == 150, "Second read must be recomputed from the latest ledger source.");
}

static LedgerBackedBalanceReadService Service(ILedgerJournalReader reader) =>
    new(reader, new BalanceProjectionService());

static JournalEntry Journal(string currency, params LedgerLine[] lines) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        currency,
        $"BAL-READ-{Guid.NewGuid():N}",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        lines);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class RecordingLedgerJournalReader(IReadOnlyList<JournalEntry> journalEntries) : ILedgerJournalReader
{
    public BalanceKey? LastKey { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<IReadOnlyList<JournalEntry>> ReadAsync(BalanceKey key, CancellationToken cancellationToken = default)
    {
        LastKey = key;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(journalEntries);
    }
}

sealed class SequencedLedgerJournalReader(IReadOnlyList<IReadOnlyList<JournalEntry>> reads) : ILedgerJournalReader
{
    private int index;

    public Task<IReadOnlyList<JournalEntry>> ReadAsync(BalanceKey key, CancellationToken cancellationToken = default)
    {
        if (index >= reads.Count)
        {
            throw new InvalidOperationException("No configured ledger read remains.");
        }

        return Task.FromResult(reads[index++]);
    }
}
