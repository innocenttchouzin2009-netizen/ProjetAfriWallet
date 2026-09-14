using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;
using AfriWallet.Transfer.Infrastructure;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
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

var sourceWalletId = Guid.NewGuid();
var targetWalletId = Guid.NewGuid();
var sourceAccountId = AccountId.New();
var targetAccountId = AccountId.New();
var correlationId = Guid.NewGuid();
var transferId = TransferId.New();
var journalId = JournalEntryId.New();
var postedAt = new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero);

var journal = JournalEntry.Create(
    journalId,
    "xaf",
    $"TRF-{transferId.Value:N}",
    correlationId,
    postedAt,
    [
        new LedgerLine(sourceAccountId, LedgerSide.Debit, 7_500, "Internal transfer debit"),
        new LedgerLine(targetAccountId, LedgerSide.Credit, 7_500, "Internal transfer credit")
    ]);

var mappings = new Dictionary<Guid, AccountId>
{
    [sourceWalletId] = sourceAccountId,
    [targetWalletId] = targetAccountId
};

var repository = new FakeJournalRepository(journal);
var reader = new LedgerBackedTransferReceiptReader(repository, mappings);
var receipt = await reader.FindByCorrelationIdAsync(correlationId);

Assert(receipt is not null, "Existing transfer journal must produce a receipt.");
Assert(receipt!.TransferId == transferId, "Receipt transfer id must come from the TRF business reference.");
Assert(receipt.JournalEntryId == journalId, "Receipt journal id must match the ledger journal.");
Assert(receipt.SourceWalletId == sourceWalletId, "Debit account must reconstruct the source wallet.");
Assert(receipt.TargetWalletId == targetWalletId, "Credit account must reconstruct the target wallet.");
Assert(receipt.CurrencyCode == "XAF", "Receipt currency must be normalized by the read model.");
Assert(receipt.AmountMinor == 7_500, "Receipt amount must match the balanced journal amount.");
Assert(receipt.CorrelationId == correlationId, "Receipt correlation must be preserved.");
Assert(receipt.CreatedAtUtc == postedAt, "Receipt timestamp must use ledger posting time.");
Assert(repository.LastCorrelationId == correlationId, "Reader must query ledger by correlation id.");

var missing = await new LedgerBackedTransferReceiptReader(new FakeJournalRepository(null), mappings)
    .FindByCorrelationIdAsync(Guid.NewGuid());
Assert(missing is null, "Missing ledger journal must return no receipt.");

await AssertThrowsAsync<InvalidOperationException>(
    () => new LedgerBackedTransferReceiptReader(
            new FakeJournalRepository(journal),
            new Dictionary<Guid, AccountId> { [sourceWalletId] = sourceAccountId })
        .FindByCorrelationIdAsync(correlationId),
    "Missing target wallet mapping must fail closed.");

var malformedReference = JournalEntry.Create(
    JournalEntryId.New(),
    "XAF",
    "NOT-A-TRANSFER",
    Guid.NewGuid(),
    postedAt,
    [
        new LedgerLine(sourceAccountId, LedgerSide.Debit, 100, "debit"),
        new LedgerLine(targetAccountId, LedgerSide.Credit, 100, "credit")
    ]);
await AssertThrowsAsync<InvalidOperationException>(
    () => new LedgerBackedTransferReceiptReader(new FakeJournalRepository(malformedReference), mappings)
        .FindByCorrelationIdAsync(malformedReference.CorrelationId),
    "Non-transfer ledger business reference must be rejected.");

var duplicateDebit = JournalEntry.Create(
    JournalEntryId.New(),
    "XAF",
    $"TRF-{TransferId.New().Value:N}",
    Guid.NewGuid(),
    postedAt,
    [
        new LedgerLine(sourceAccountId, LedgerSide.Debit, 50, "debit-1"),
        new LedgerLine(sourceAccountId, LedgerSide.Debit, 50, "debit-2"),
        new LedgerLine(targetAccountId, LedgerSide.Credit, 100, "credit")
    ]);
await AssertThrowsAsync<InvalidOperationException>(
    () => new LedgerBackedTransferReceiptReader(new FakeJournalRepository(duplicateDebit), mappings)
        .FindByCorrelationIdAsync(duplicateDebit.CorrelationId),
    "Receipt reconstruction must require exactly one debit line.");

var duplicateMappings = new Dictionary<Guid, AccountId>
{
    [Guid.NewGuid()] = sourceAccountId,
    [Guid.NewGuid()] = sourceAccountId
};
try
{
    _ = new LedgerBackedTransferReceiptReader(repository, duplicateMappings);
    throw new InvalidOperationException("Expected duplicate account mapping rejection.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("only one wallet", StringComparison.Ordinal)) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => reader.FindByCorrelationIdAsync(correlationId, cts.Token),
    "Cancelled receipt lookup must stop before ledger access.");

Console.WriteLine("AFW-BE-TRANSFER-CORRELATION-1 ledger-backed receipt reader scenarios: PASS");

sealed class FakeJournalRepository(JournalEntry? journal) : IJournalRepository
{
    public Guid? LastCorrelationId { get; private set; }

    public Task<bool> ExistsByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(journal?.CorrelationId == correlationId);

    public Task AddAsync(
        JournalEntry journalEntry,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Read-only receipt reader must never add ledger journals.");

    public Task<JournalEntry?> GetAsync(
        JournalEntryId journalEntryId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(journal?.Id == journalEntryId ? journal : null);

    public Task<JournalEntry?> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastCorrelationId = correlationId;
        return Task.FromResult(journal?.CorrelationId == correlationId ? journal : null);
    }
}
