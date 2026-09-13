using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Reconciliation.Application;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var transferId = Guid.NewGuid();
var correlationId = Guid.NewGuid();
var journalId = JournalEntryId.New();
var debitAccount = new AccountId(Guid.NewGuid());
var creditAccount = new AccountId(Guid.NewGuid());
var postedAt = new DateTimeOffset(2026, 9, 13, 5, 0, 0, TimeSpan.Zero);

var transferJournal = JournalEntry.Create(
    journalId,
    "xaf",
    $"TRF-{transferId:N}",
    correlationId,
    postedAt,
    [
        new LedgerLine(debitAccount, LedgerSide.Debit, 15_000, "Internal transfer debit"),
        new LedgerLine(creditAccount, LedgerSide.Credit, 15_000, "Internal transfer credit")
    ]);

var repository = new InMemoryJournalRepository([transferJournal]);
var service = new TransferReceiptLookupService(repository);

var byCorrelation = await service.GetByCorrelationIdAsync(correlationId);
Assert(byCorrelation.Status == TransferReceiptLookupStatus.Found, "Transfer must be found by correlation id.");
Assert(byCorrelation.Receipt is not null, "Receipt is required.");
Assert(byCorrelation.Receipt!.TransferId == transferId, "Transfer id mismatch.");
Assert(byCorrelation.Receipt.JournalEntryId == journalId, "Journal id mismatch.");
Assert(byCorrelation.Receipt.CurrencyCode == "XAF", "Currency mismatch.");
Assert(byCorrelation.Receipt.AmountMinor == 15_000, "Amount mismatch.");
Assert(byCorrelation.Receipt.DebitAccountId == debitAccount, "Debit account mismatch.");
Assert(byCorrelation.Receipt.CreditAccountId == creditAccount, "Credit account mismatch.");
Assert(byCorrelation.Receipt.PostedAtUtc == postedAt, "Posted timestamp mismatch.");

var byJournal = await service.GetByJournalEntryIdAsync(journalId.Value);
Assert(byJournal.Status == TransferReceiptLookupStatus.Found, "Transfer must be found by journal id.");
Assert(byJournal.Receipt?.CorrelationId == correlationId, "Correlation mismatch on journal lookup.");

var missing = await service.GetByCorrelationIdAsync(Guid.NewGuid());
Assert(missing.Status == TransferReceiptLookupStatus.NotFound, "Missing correlation must return NotFound.");

var nonTransfer = JournalEntry.Create(
    JournalEntryId.New(),
    "XAF",
    "FEE-001",
    Guid.NewGuid(),
    postedAt,
    [
        new LedgerLine(new AccountId(Guid.NewGuid()), LedgerSide.Debit, 100),
        new LedgerLine(new AccountId(Guid.NewGuid()), LedgerSide.Credit, 100)
    ]);
repository.AddDirect(nonTransfer);
var nonTransferResult = await service.GetByCorrelationIdAsync(nonTransfer.CorrelationId);
Assert(nonTransferResult.Status == TransferReceiptLookupStatus.NotTransferJournal, "Non-transfer journal must be distinguished.");

var malformedReference = JournalEntry.Create(
    JournalEntryId.New(),
    "XAF",
    "TRF-not-a-guid",
    Guid.NewGuid(),
    postedAt,
    [
        new LedgerLine(new AccountId(Guid.NewGuid()), LedgerSide.Debit, 200),
        new LedgerLine(new AccountId(Guid.NewGuid()), LedgerSide.Credit, 200)
    ]);
repository.AddDirect(malformedReference);
var malformedResult = await service.GetByCorrelationIdAsync(malformedReference.CorrelationId);
Assert(malformedResult.Status == TransferReceiptLookupStatus.InvalidTransferJournal, "Malformed transfer reference must fail closed.");

try
{
    await service.GetByCorrelationIdAsync(Guid.Empty);
    throw new InvalidOperationException("Expected empty correlation validation failure.");
}
catch (ArgumentException) { }

try
{
    await service.GetByJournalEntryIdAsync(Guid.Empty);
    throw new InvalidOperationException("Expected empty journal id validation failure.");
}
catch (ArgumentException) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.GetByCorrelationIdAsync(correlationId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-RECON-1 transfer receipt lookup and reconciliation foundation scenarios: PASS");

sealed class InMemoryJournalRepository(IEnumerable<JournalEntry> seed) : IJournalRepository
{
    private readonly Dictionary<Guid, JournalEntry> byId = seed.ToDictionary(entry => entry.Id.Value);
    private readonly Dictionary<Guid, JournalEntry> byCorrelation = seed.ToDictionary(entry => entry.CorrelationId);

    public void AddDirect(JournalEntry entry)
    {
        byId[entry.Id.Value] = entry;
        byCorrelation[entry.CorrelationId] = entry;
    }

    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(byCorrelation.ContainsKey(correlationId));
    }

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddDirect(journalEntry);
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.TryGetValue(journalEntryId.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byCorrelation.TryGetValue(correlationId, out var value);
        return Task.FromResult(value);
    }
}
