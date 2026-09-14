using AfriWallet.Ledger.Domain;
using AfriWallet.PaymentRequests.Reconciliation.Infrastructure;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;
using CertifiedTransferReceiptReader = AfriWallet.Transfer.Application.ITransferReceiptReader;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
{
    try { await action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}

var transferId = TransferId.New();
var journalEntryId = JournalEntryId.New();
var sourceWalletId = Guid.NewGuid();
var targetWalletId = Guid.NewGuid();
var correlationId = Guid.NewGuid();
var createdAt = new DateTimeOffset(2026, 9, 14, 15, 30, 0, TimeSpan.Zero);
var certifiedReceipt = TransferReceiptReadModel.Create(
    transferId,
    journalEntryId,
    sourceWalletId,
    targetWalletId,
    "XAF",
    25_000,
    correlationId,
    createdAt);

var reader = new RecordingCertifiedReader(certifiedReceipt);
var adapter = new TransferReceiptReaderAdapter(reader);
var snapshot = await adapter.FindByCorrelationIdAsync(correlationId);
Assert(snapshot is not null, "Certified transfer receipt must map to reconciliation snapshot.");
Assert(reader.LastCorrelationId == correlationId, "Adapter must forward the exact correlation id.");
Assert(snapshot!.TransferId == transferId.Value, "Transfer id mismatch.");
Assert(snapshot.SourceWalletId == sourceWalletId, "Source wallet mismatch.");
Assert(snapshot.TargetWalletId == targetWalletId, "Target wallet mismatch.");
Assert(snapshot.AmountMinor == 25_000, "Amount mismatch.");
Assert(snapshot.CorrelationId == correlationId, "Correlation mismatch.");
Assert(snapshot.CreatedAtUtc == createdAt, "Created timestamp mismatch.");

var missing = await new TransferReceiptReaderAdapter(new RecordingCertifiedReader(null))
    .FindByCorrelationIdAsync(Guid.NewGuid());
Assert(missing is null, "Missing certified receipt must remain missing.");

await AssertThrowsAsync<ArgumentException>(
    () => adapter.FindByCorrelationIdAsync(Guid.Empty),
    "Empty correlation id must be rejected before calling certified reader.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => adapter.FindByCorrelationIdAsync(Guid.NewGuid(), cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-REQUEST-RECONCILE-1 certified transfer receipt adapter scenarios: PASS");

sealed class RecordingCertifiedReader(TransferReceiptReadModel? receipt) : CertifiedTransferReceiptReader
{
    public Guid? LastCorrelationId { get; private set; }

    public Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastCorrelationId = correlationId;
        return Task.FromResult(receipt);
    }
}
