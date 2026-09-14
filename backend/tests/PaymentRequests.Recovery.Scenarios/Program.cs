using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

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

var requester = WalletId.From(Guid.NewGuid());
var payer = WalletId.From(Guid.NewGuid());
var createdAt = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
var acceptedAt = createdAt.AddMinutes(2);
var amountMinor = 7_500L;

PaymentRequest NewPending() => PaymentRequest.Create(
    requester,
    RecipientReference.FromAfWalId("payer.recovery"),
    Currency.Create("EUR"),
    amountMinor,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddHours(2));

var missingRepo = new InMemoryRepository();
var missingPort = new RecordingReconciliationPort(null);
var missingService = new PaymentRequestRecoveryService(missingRepo, missingPort);
var missing = await missingService.ReconcileAsync(PaymentRequestId.From(Guid.NewGuid()));
Assert(missing.Status == PaymentRequestRecoveryStatus.RequestNotFound, "Unknown request must return RequestNotFound.");
Assert(missingPort.Calls == 0, "Unknown request must not query reconciliation evidence.");

var pending = NewPending();
var pendingRepo = new InMemoryRepository(pending);
var pendingPort = new RecordingReconciliationPort(null);
var pendingService = new PaymentRequestRecoveryService(pendingRepo, pendingPort);
var notEligible = await pendingService.ReconcileAsync(pending.Id);
Assert(notEligible.Status == PaymentRequestRecoveryStatus.NotEligible, "Pending request must not be auto-reconciled.");
Assert(pendingPort.Calls == 0, "Ineligible request must not query reconciliation evidence.");
Assert(pendingRepo.UpdateCalls == 0, "Ineligible request must not be updated.");

var acceptedWithoutPayment = NewPending();
acceptedWithoutPayment.Accept(payer, acceptedAt);
var noPaymentRepo = new InMemoryRepository(acceptedWithoutPayment);
var noPaymentPort = new RecordingReconciliationPort(null);
var noPaymentService = new PaymentRequestRecoveryService(noPaymentRepo, noPaymentPort);
var noPayment = await noPaymentService.ReconcileAsync(acceptedWithoutPayment.Id);
Assert(noPayment.Status == PaymentRequestRecoveryStatus.PaymentNotFound, "Missing transfer evidence must return PaymentNotFound.");
Assert(noPayment.Request?.Status == PaymentRequestStatus.Accepted, "Missing transfer evidence must leave request Accepted.");
Assert(noPaymentPort.LastCorrelationId == acceptedWithoutPayment.Id.Value, "Recovery must query by payment request id correlation.");
Assert(noPaymentRepo.UpdateCalls == 0, "Missing transfer evidence must not mutate the request.");

var accepted = NewPending();
accepted.Accept(payer, acceptedAt);
var transferId = Guid.NewGuid();
var receipt = new PaymentRequestPaymentReceipt(
    transferId,
    payer.Value,
    requester.Value,
    amountMinor,
    accepted.Id.Value,
    acceptedAt.AddMinutes(1));
var acceptedRepo = new InMemoryRepository(accepted);
var acceptedPort = new RecordingReconciliationPort(receipt);
var recoveryService = new PaymentRequestRecoveryService(acceptedRepo, acceptedPort);
var reconciled = await recoveryService.ReconcileAsync(accepted.Id);
Assert(reconciled.Status == PaymentRequestRecoveryStatus.Reconciled, "Matching transfer evidence must reconcile the request.");
Assert(reconciled.Request?.Status == PaymentRequestStatus.Paid, "Reconciled request must become Paid.");
Assert(reconciled.Request?.TransferId == transferId, "Reconciled transfer id must be persisted.");
Assert(acceptedRepo.UpdateCalls == 1, "Successful recovery must update exactly once.");

var alreadyPaid = NewPending();
alreadyPaid.Accept(payer, acceptedAt);
alreadyPaid.MarkPaid(Guid.NewGuid(), acceptedAt.AddMinutes(1));
var alreadyRepo = new InMemoryRepository(alreadyPaid);
var alreadyPort = new RecordingReconciliationPort(null);
var alreadyService = new PaymentRequestRecoveryService(alreadyRepo, alreadyPort);
var already = await alreadyService.ReconcileAsync(alreadyPaid.Id);
Assert(already.Status == PaymentRequestRecoveryStatus.AlreadyPaid, "Paid request must be idempotent.");
Assert(alreadyPort.Calls == 0, "Already-paid request must not query transfer evidence.");
Assert(alreadyRepo.UpdateCalls == 0, "Already-paid request must not be rewritten.");

var mismatch = NewPending();
mismatch.Accept(payer, acceptedAt);
var mismatchReceipt = receipt with
{
    SourceWalletId = Guid.NewGuid(),
    CorrelationId = mismatch.Id.Value
};
var mismatchService = new PaymentRequestRecoveryService(
    new InMemoryRepository(mismatch),
    new RecordingReconciliationPort(mismatchReceipt));
await AssertThrowsAsync<InvalidOperationException>(
    () => mismatchService.ReconcileAsync(mismatch.Id),
    "Mismatched transfer evidence must fail closed.");

var tooEarly = NewPending();
tooEarly.Accept(payer, acceptedAt);
var earlyReceipt = new PaymentRequestPaymentReceipt(
    Guid.NewGuid(), payer.Value, requester.Value, amountMinor, tooEarly.Id.Value, acceptedAt.AddSeconds(-1));
var earlyService = new PaymentRequestRecoveryService(
    new InMemoryRepository(tooEarly),
    new RecordingReconciliationPort(earlyReceipt));
await AssertThrowsAsync<InvalidOperationException>(
    () => earlyService.ReconcileAsync(tooEarly.Id),
    "Transfer evidence predating acceptance must fail closed.");

var cancelled = NewPending();
cancelled.Cancel(createdAt.AddMinutes(1));
var cancelledPort = new RecordingReconciliationPort(receipt with { CorrelationId = cancelled.Id.Value });
var cancelledService = new PaymentRequestRecoveryService(new InMemoryRepository(cancelled), cancelledPort);
var cancelledResult = await cancelledService.ReconcileAsync(cancelled.Id);
Assert(cancelledResult.Status == PaymentRequestRecoveryStatus.NotEligible, "Cancelled request must not be auto-reconciled.");
Assert(cancelledPort.Calls == 0, "Terminal non-paid request must not query transfer evidence.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => recoveryService.ReconcileAsync(accepted.Id, cts.Token),
    "Cancellation must propagate before repository access.");

Console.WriteLine("AFW-BE-REQUEST-RECOVERY-1 application reconciliation scenarios: PASS");

sealed class InMemoryRepository(params PaymentRequest[] requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> values = requests.ToDictionary(x => x.Id.Value);
    public int UpdateCalls { get; private set; }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.Values.FirstOrDefault(x => x.CorrelationId == correlationId));
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(request.Id.Value, request);
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

sealed class RecordingReconciliationPort(PaymentRequestPaymentReceipt? receipt) : IPaymentRequestReconciliationPort
{
    public int Calls { get; private set; }
    public Guid? LastCorrelationId { get; private set; }

    public Task<PaymentRequestPaymentReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCorrelationId = correlationId;
        return Task.FromResult(receipt);
    }
}
