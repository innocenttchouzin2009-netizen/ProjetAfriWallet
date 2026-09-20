using AfriWallet.Ledger.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var correlationId = Guid.NewGuid();
var transferId = TransferId.New();
var sourceWalletId = Guid.NewGuid();
var targetWalletId = Guid.NewGuid();
var createdAtUtc = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);

var transferReceipt = TransferReceiptReadModel.Create(
    transferId,
    JournalEntryId.New(),
    sourceWalletId,
    targetWalletId,
    "XAF",
    12_500,
    correlationId,
    createdAtUtc);

var reader = new FakeReceiptReader(transferReceipt);
var lookup = new TransferCorrelationLookupService(reader);
var port = new TransferCorrelationPaymentRequestReconciliationPort(lookup);

var mapped = await port.FindByCorrelationIdAsync(correlationId);
Assert(mapped is not null, "Expected reconciliation receipt.");
Assert(mapped!.TransferId == transferId.Value, "Transfer id must be mapped.");
Assert(mapped.SourceWalletId == sourceWalletId, "Source wallet must be mapped.");
Assert(mapped.TargetWalletId == targetWalletId, "Target wallet must be mapped.");
Assert(mapped.AmountMinor == 12_500, "Amount must be mapped.");
Assert(mapped.CorrelationId == correlationId, "Correlation id must be mapped.");
Assert(mapped.CreatedAtUtc == createdAtUtc, "Creation timestamp must be mapped.");
Assert(reader.Calls == 1, "Adapter must perform exactly one read lookup.");
Assert(reader.LastCorrelationId == correlationId, "Adapter must forward correlation id unchanged.");

var missingReader = new FakeReceiptReader(null);
var missingPort = new TransferCorrelationPaymentRequestReconciliationPort(
    new TransferCorrelationLookupService(missingReader));
var missing = await missingPort.FindByCorrelationIdAsync(Guid.NewGuid());
Assert(missing is null, "Missing transfer receipt must map to null.");
Assert(missingReader.Calls == 1, "Missing lookup must still perform one read.");

try
{
    await port.FindByCorrelationIdAsync(Guid.Empty);
    throw new InvalidOperationException("Expected empty correlation validation failure.");
}
catch (ArgumentException) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await port.FindByCorrelationIdAsync(correlationId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-RECOVERY-1 transfer correlation reconciliation adapter scenarios: PASS");

sealed class FakeReceiptReader(TransferReceiptReadModel? receipt) : ITransferReceiptReader
{
    public int Calls { get; private set; }
    public Guid? LastCorrelationId { get; private set; }

    public Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCorrelationId = correlationId;
        return Task.FromResult(receipt);
    }
}
