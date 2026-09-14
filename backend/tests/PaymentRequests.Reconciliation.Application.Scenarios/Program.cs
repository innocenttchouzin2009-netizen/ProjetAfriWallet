using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Reconciliation.Application;
using AfriWallet.Wallet.Domain;

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

var requesterWallet = WalletId.From(Guid.NewGuid());
var payerWallet = WalletId.From(Guid.NewGuid());
var createdAt = new DateTimeOffset(2026, 9, 14, 14, 0, 0, TimeSpan.Zero);
var acceptedAt = createdAt.AddMinutes(1);
var receiptAt = acceptedAt.AddSeconds(5);

PaymentRequest AcceptedRequest()
{
    var request = PaymentRequest.Create(
        requesterWallet,
        RecipientReference.FromAfWalId("payer.afwal"),
        Currency.Create("EUR"),
        5_000,
        Guid.NewGuid(),
        createdAt,
        createdAt.AddHours(1));
    request.Accept(payerWallet, acceptedAt);
    return request;
}

var accepted = AcceptedRequest();
var transferId = Guid.NewGuid();
var repository = new InMemoryRepository(accepted);
var receiptReader = new FixedReceiptReader(new TransferReceiptSnapshot(
    transferId,
    payerWallet.Value,
    requesterWallet.Value,
    accepted.AmountMinor,
    accepted.Id.Value,
    receiptAt));
var records = new InMemoryReconciliationRecordRepository();
var service = new PaymentRequestReconciliationService(repository, receiptReader, records);

var reconciled = await service.ReconcileAsync(accepted.Id);
Assert(reconciled.Status == PaymentRequestReconciliationStatus.Reconciled, "Accepted request with matching receipt must reconcile.");
Assert(reconciled.Request?.Status == PaymentRequestStatus.Paid, "Reconciled request must be paid.");
Assert(reconciled.Request?.TransferId == transferId, "Transfer id must be persisted on request.");
Assert(repository.UpdateCalls == 1, "Reconciliation must persist exactly one request update.");
Assert(records.Get(accepted.Id)?.Status == PaymentRequestReconciliationStatus.Reconciled, "Reconciliation record must be durable through the record port.");
Assert(records.Get(accepted.Id)?.TransferId == transferId, "Reconciliation record must preserve transfer id.");
Assert(receiptReader.LastCorrelationId == accepted.Id.Value, "Receipt lookup must use deterministic request id correlation.");

var replay = await service.ReconcileAsync(accepted.Id);
Assert(replay.Status == PaymentRequestReconciliationStatus.AlreadyPaid, "Paid request reconciliation must be idempotent.");
Assert(repository.UpdateCalls == 1, "Paid replay must not update request again.");
Assert(receiptReader.Calls == 1, "Paid replay must not query receipt again.");
Assert(records.Count == 1, "Replay must upsert one reconciliation record, not add duplicates.");
Assert(records.Get(accepted.Id)?.Status == PaymentRequestReconciliationStatus.AlreadyPaid, "Replay must refresh durable status.");

var missingRecords = new InMemoryReconciliationRecordRepository();
var missingId = PaymentRequestId.From(Guid.NewGuid());
var missingRequestService = new PaymentRequestReconciliationService(new InMemoryRepository(), new FixedReceiptReader(null), missingRecords);
var missingRequest = await missingRequestService.ReconcileAsync(missingId);
Assert(missingRequest.Status == PaymentRequestReconciliationStatus.NotFound, "Unknown request must return NotFound.");
Assert(missingRecords.Get(missingId)?.Status == PaymentRequestReconciliationStatus.NotFound, "NotFound must be recorded.");

var pending = PaymentRequest.Create(
    requesterWallet,
    RecipientReference.FromQrToken("opaque-qr"),
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddHours(1));
var pendingReader = new FixedReceiptReader(null);
var pendingRecords = new InMemoryReconciliationRecordRepository();
var pendingResult = await new PaymentRequestReconciliationService(new InMemoryRepository(pending), pendingReader, pendingRecords).ReconcileAsync(pending.Id);
Assert(pendingResult.Status == PaymentRequestReconciliationStatus.NotEligible, "Pending request must not reconcile.");
Assert(pendingReader.Calls == 0, "Ineligible request must not query transfer receipts.");
Assert(pendingRecords.Get(pending.Id)?.Status == PaymentRequestReconciliationStatus.NotEligible, "NotEligible must be recorded.");

var noReceipt = AcceptedRequest();
var noReceiptRecords = new InMemoryReconciliationRecordRepository();
var noReceiptResult = await new PaymentRequestReconciliationService(new InMemoryRepository(noReceipt), new FixedReceiptReader(null), noReceiptRecords).ReconcileAsync(noReceipt.Id);
Assert(noReceiptResult.Status == PaymentRequestReconciliationStatus.TransferReceiptNotFound, "Missing receipt must be explicit.");
Assert(noReceipt.Status == PaymentRequestStatus.Accepted, "Missing receipt must leave request accepted.");
Assert(noReceiptRecords.Get(noReceipt.Id)?.Status == PaymentRequestReconciliationStatus.TransferReceiptNotFound, "Missing receipt state must be recorded for later retry.");

var mismatch = AcceptedRequest();
var mismatchRecords = new InMemoryReconciliationRecordRepository();
var mismatchService = new PaymentRequestReconciliationService(
    new InMemoryRepository(mismatch),
    new FixedReceiptReader(new TransferReceiptSnapshot(
        Guid.NewGuid(), payerWallet.Value, requesterWallet.Value, mismatch.AmountMinor + 1, mismatch.Id.Value, receiptAt)),
    mismatchRecords);
await AssertThrowsAsync<InvalidOperationException>(
    () => mismatchService.ReconcileAsync(mismatch.Id),
    "Mismatched transfer receipt must fail closed.");
Assert(mismatch.Status == PaymentRequestStatus.Accepted, "Mismatch must not mark request paid.");
Assert(mismatchRecords.Count == 0, "Mismatch must not persist a false successful state.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.ReconcileAsync(accepted.Id, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-REQUEST-RECONCILE-1 application durable-state scenarios: PASS");

sealed class InMemoryRepository(params PaymentRequest[] requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> values = requests.ToDictionary(x => x.Id.Value);
    public int UpdateCalls { get; private set; }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var request);
        return Task.FromResult(request);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.Values.FirstOrDefault(x => x.CorrelationId == correlationId));
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[request.Id.Value] = request;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[request.Id.Value] = request;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

sealed class InMemoryReconciliationRecordRepository : IPaymentRequestReconciliationRecordRepository
{
    private readonly Dictionary<Guid, PaymentRequestReconciliationRecord> values = new();
    public int Count => values.Count;
    public PaymentRequestReconciliationRecord? Get(PaymentRequestId id) => values.GetValueOrDefault(id.Value);

    public Task<PaymentRequestReconciliationRecord?> GetAsync(PaymentRequestId requestId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Get(requestId));
    }

    public Task UpsertAsync(PaymentRequestReconciliationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[record.RequestId.Value] = record;
        return Task.CompletedTask;
    }
}

sealed class FixedReceiptReader(TransferReceiptSnapshot? receipt) : ITransferReceiptReader
{
    public int Calls { get; private set; }
    public Guid? LastCorrelationId { get; private set; }

    public Task<TransferReceiptSnapshot?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCorrelationId = correlationId;
        return Task.FromResult(receipt);
    }
}
