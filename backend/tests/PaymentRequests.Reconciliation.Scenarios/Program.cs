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

var requesterWallet = WalletId.From(Guid.NewGuid());
var payerWallet = WalletId.From(Guid.NewGuid());
var createdAt = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
var acceptedAt = createdAt.AddMinutes(1);
var paidAt = acceptedAt.AddMinutes(1);

PaymentRequest NewAcceptedRequest() => CreateAcceptedRequest(
    requesterWallet,
    payerWallet,
    createdAt,
    acceptedAt);

// Matching ledger-backed receipt reconciles durable Accepted -> Paid.
{
    var request = NewAcceptedRequest();
    var transferId = Guid.NewGuid();
    var receipt = MatchingReceipt(request, transferId, paidAt);
    var repository = new InMemoryRepository(request);
    var reader = new FakeReceiptReader(receipt);
    var service = new PaymentRequestReconciliationService(repository, reader);

    var result = await service.ReconcileAsync(request.Id);

    Assert(result.Status == PaymentRequestReconciliationStatus.Reconciled, "Accepted request must reconcile.");
    Assert(result.Request?.Status == PaymentRequestStatus.Paid, "Reconciled request must be Paid.");
    Assert(result.Request?.TransferId == transferId, "Reconciled request must preserve transfer id.");
    Assert(reader.LastCorrelationId == request.Id.Value, "Lookup correlation must equal payment request id.");
    Assert(repository.UpdateCalls == 1, "Reconciliation must persist Paid exactly once.");
}

// No receipt leaves Accepted untouched and performs no persistence update.
{
    var request = NewAcceptedRequest();
    var repository = new InMemoryRepository(request);
    var reader = new FakeReceiptReader(null);
    var service = new PaymentRequestReconciliationService(repository, reader);

    var result = await service.ReconcileAsync(request.Id);

    Assert(result.Status == PaymentRequestReconciliationStatus.TransferNotFound, "Missing receipt must be reported.");
    Assert(result.Request?.Status == PaymentRequestStatus.Accepted, "Missing receipt must leave request Accepted.");
    Assert(repository.UpdateCalls == 0, "Missing receipt must not update persistence.");
}

// Already Paid is idempotent and does not query transfer history.
{
    var request = NewAcceptedRequest();
    request.MarkPaid(Guid.NewGuid(), paidAt);
    var repository = new InMemoryRepository(request);
    var reader = new FakeReceiptReader(null);
    var service = new PaymentRequestReconciliationService(repository, reader);

    var result = await service.ReconcileAsync(request.Id);

    Assert(result.Status == PaymentRequestReconciliationStatus.AlreadyPaid, "Paid request must be idempotent.");
    Assert(reader.Calls == 0, "Paid request must not query transfer history.");
    Assert(repository.UpdateCalls == 0, "Paid request must not persist again.");
}

// Pending and terminal non-paid states are not eligible.
{
    var pending = PaymentRequest.Create(
        requesterWallet,
        RecipientReference.FromAfWalId("payer.one"),
        Currency.Create("XAF"),
        2_500,
        Guid.NewGuid(),
        createdAt,
        createdAt.AddHours(1));
    var repository = new InMemoryRepository(pending);
    var reader = new FakeReceiptReader(null);
    var service = new PaymentRequestReconciliationService(repository, reader);

    var result = await service.ReconcileAsync(pending.Id);

    Assert(result.Status == PaymentRequestReconciliationStatus.NotEligible, "Pending request must not reconcile.");
    Assert(reader.Calls == 0, "Ineligible request must not query transfer history.");
}

// Missing request fails closed without transfer lookup.
{
    var repository = new InMemoryRepository();
    var reader = new FakeReceiptReader(null);
    var service = new PaymentRequestReconciliationService(repository, reader);

    var result = await service.ReconcileAsync(PaymentRequestId.From(Guid.NewGuid()));

    Assert(result.Status == PaymentRequestReconciliationStatus.RequestNotFound, "Missing request must return RequestNotFound.");
    Assert(reader.Calls == 0, "Missing request must not query transfer history.");
}

// Any mismatched receipt fails closed before state mutation.
{
    var request = NewAcceptedRequest();
    var mismatched = MatchingReceipt(request, Guid.NewGuid(), paidAt) with { AmountMinor = request.AmountMinor + 1 };
    var repository = new InMemoryRepository(request);
    var service = new PaymentRequestReconciliationService(repository, new FakeReceiptReader(mismatched));

    await AssertThrowsAsync<InvalidOperationException>(
        () => service.ReconcileAsync(request.Id),
        "Mismatched receipt must fail closed.");

    Assert(request.Status == PaymentRequestStatus.Accepted, "Mismatched receipt must not mutate request.");
    Assert(repository.UpdateCalls == 0, "Mismatched receipt must not update persistence.");
}

// Receipt predating acceptance is invalid proof.
{
    var request = NewAcceptedRequest();
    var receipt = MatchingReceipt(request, Guid.NewGuid(), createdAt);
    var repository = new InMemoryRepository(request);
    var service = new PaymentRequestReconciliationService(repository, new FakeReceiptReader(receipt));

    await AssertThrowsAsync<InvalidOperationException>(
        () => service.ReconcileAsync(request.Id),
        "Receipt before acceptance must fail closed.");

    Assert(request.Status == PaymentRequestStatus.Accepted, "Invalid timestamp must leave request Accepted.");
    Assert(repository.UpdateCalls == 0, "Invalid timestamp must not update persistence.");
}

// Cancellation is honored before repository access.
{
    var request = NewAcceptedRequest();
    var repository = new InMemoryRepository(request);
    var service = new PaymentRequestReconciliationService(repository, new FakeReceiptReader(null));
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await AssertThrowsAsync<OperationCanceledException>(
        () => service.ReconcileAsync(request.Id, cts.Token),
        "Cancellation must propagate.");

    Assert(repository.GetCalls == 0, "Pre-cancelled reconciliation must not access repository.");
}

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-1 application foundation scenarios: PASS");

static PaymentRequest CreateAcceptedRequest(
    WalletId requesterWallet,
    WalletId payerWallet,
    DateTimeOffset createdAt,
    DateTimeOffset acceptedAt)
{
    var request = PaymentRequest.Create(
        requesterWallet,
        RecipientReference.FromAfWalId("payer.one"),
        Currency.Create("XAF"),
        2_500,
        Guid.NewGuid(),
        createdAt,
        createdAt.AddHours(1));
    request.Accept(payerWallet, acceptedAt);
    return request;
}

static PaymentRequestReconciliationReceipt MatchingReceipt(
    PaymentRequest request,
    Guid transferId,
    DateTimeOffset createdAtUtc) => new(
        transferId,
        request.AcceptedPayerWalletId!.Value.Value,
        request.RequesterWalletId.Value,
        request.Currency.Code,
        request.AmountMinor,
        request.Id.Value,
        createdAtUtc);

sealed class FakeReceiptReader(PaymentRequestReconciliationReceipt? receipt) : IPaymentRequestPaymentReceiptReader
{
    public int Calls { get; private set; }
    public Guid? LastCorrelationId { get; private set; }

    public Task<PaymentRequestReconciliationReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCorrelationId = correlationId;
        return Task.FromResult(receipt);
    }
}

sealed class InMemoryRepository : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> byId = new();
    private readonly Dictionary<Guid, PaymentRequest> byCorrelation = new();

    public InMemoryRepository(params PaymentRequest[] requests)
    {
        foreach (var request in requests)
        {
            byId[request.Id.Value] = request;
            byCorrelation[request.CorrelationId] = request;
        }
    }

    public int GetCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetCalls++;
        byId.TryGetValue(id.Value, out var request);
        return Task.FromResult(request);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byCorrelation.TryGetValue(correlationId, out var request);
        return Task.FromResult(request);
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.Add(request.Id.Value, request);
        byCorrelation.Add(request.CorrelationId, request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}
