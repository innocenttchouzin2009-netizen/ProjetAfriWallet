using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
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
var currency = Currency.Create("EUR");
var recipient = RecipientReference.FromAfWalId("payer.afwal");
var createdAt = new DateTimeOffset(2026, 9, 14, 19, 30, 0, TimeSpan.Zero);
var actor = Guid.Parse("11111111-1111-1111-1111-111111111111");
var requesterOwner = Guid.Parse("22222222-2222-2222-2222-222222222222");

var publisher = new RecordingPublisher();
var dispatcher = new PaymentRequestEventDispatcher(publisher);
var repository = new RecordingRepository();
var resolver = new FixedRecipientResolver(payer);
var application = new PaymentRequestApplicationService(repository, resolver, dispatcher);

var correlationId = Guid.NewGuid();
var created = await application.CreateAsync(new CreatePaymentRequestCommand(
    requester,
    recipient,
    currency,
    5_000,
    correlationId,
    createdAt,
    createdAt.AddHours(1)));
Assert(created.Status == CreatePaymentRequestStatus.Created && created.Request is not null, "Request must be created.");
Assert(repository.AddCalls == 1, "Created request must be persisted first.");
Assert(publisher.Events.Count == 1 && publisher.Events[0].Kind == PaymentRequestEventKind.Created, "Created event must be emitted once.");
Assert(publisher.Events[0].PaymentRequestId == created.Request!.Id.Value, "Created event request id mismatch.");

var replay = await application.CreateAsync(new CreatePaymentRequestCommand(
    requester,
    recipient,
    currency,
    5_000,
    correlationId,
    createdAt,
    createdAt.AddHours(1)));
Assert(replay.Status == CreatePaymentRequestStatus.Existing, "Replay must return existing request.");
Assert(publisher.Events.Count == 1, "Idempotent replay must not re-emit Created.");

var ownership = new MultiOwnershipReader(new Dictionary<Guid, Guid>
{
    [payer.Value] = actor,
    [requester.Value] = requesterOwner
});
var payment = new RecordingPaymentPort();
var actions = new PaymentRequestActionService(repository, resolver, ownership, payment, dispatcher);

var declineRequest = PaymentRequest.Create(requester, recipient, currency, 1_000, Guid.NewGuid(), createdAt);
await repository.AddAsync(declineRequest);
var beforeDecline = publisher.Events.Count;
var declined = await actions.DeclineAsync(declineRequest.Id, actor, createdAt.AddMinutes(2));
Assert(declined.Status == PaymentRequestActionStatus.Success, "Decline must succeed.");
Assert(repository.UpdateCalls >= 1, "Decline must persist.");
Assert(publisher.Events.Count == beforeDecline + 1 && publisher.Events[^1].Kind == PaymentRequestEventKind.Declined, "Declined event missing.");

var cancelRequest = PaymentRequest.Create(requester, recipient, currency, 1_000, Guid.NewGuid(), createdAt);
await repository.AddAsync(cancelRequest);
var beforeCancel = publisher.Events.Count;
var cancelled = await actions.CancelAsync(cancelRequest.Id, requesterOwner, createdAt.AddMinutes(3));
Assert(cancelled.Status == PaymentRequestActionStatus.Success, "Cancel must succeed.");
Assert(publisher.Events.Count == beforeCancel + 1 && publisher.Events[^1].Kind == PaymentRequestEventKind.Cancelled, "Cancelled event missing.");

var payRequest = PaymentRequest.Create(requester, recipient, currency, 2_500, Guid.NewGuid(), createdAt, createdAt.AddHours(2));
await repository.AddAsync(payRequest);
var beforePay = publisher.Events.Count;
var paid = await actions.AcceptAndPayAsync(payRequest.Id, actor, createdAt.AddMinutes(4));
Assert(paid.Status == PaymentRequestActionStatus.Success && paid.Request?.Status == PaymentRequestStatus.Paid, "Accept-and-pay must succeed.");
Assert(publisher.Events.Count == beforePay + 2, "Accept-and-pay must emit exactly Accepted and Paid.");
Assert(publisher.Events[^2].Kind == PaymentRequestEventKind.Accepted, "Accepted event missing.");
Assert(publisher.Events[^1].Kind == PaymentRequestEventKind.Paid, "Paid event missing.");
Assert(publisher.Events[^1].TransferId == payment.LastReceipt?.TransferId, "Paid event must carry transfer id.");

var afterPaidCount = publisher.Events.Count;
var paidReplay = await actions.AcceptAndPayAsync(payRequest.Id, actor, createdAt.AddMinutes(5));
Assert(paidReplay.Status == PaymentRequestActionStatus.Success, "Paid replay must succeed idempotently.");
Assert(publisher.Events.Count == afterPaidCount, "Paid replay must not re-emit Accepted or Paid.");

var expiring = PaymentRequest.Create(requester, recipient, currency, 1_000, Guid.NewGuid(), createdAt, createdAt.AddMinutes(10));
await repository.AddAsync(expiring);
var beforeExpire = publisher.Events.Count;
var expired = await actions.ExpireAsync(expiring.Id, createdAt.AddMinutes(10));
Assert(expired.Status == PaymentRequestActionStatus.Success && expired.Request?.Status == PaymentRequestStatus.Expired, "Expire must succeed.");
Assert(publisher.Events.Count == beforeExpire + 1 && publisher.Events[^1].Kind == PaymentRequestEventKind.Expired, "Expired event missing.");
var afterExpiredCount = publisher.Events.Count;
await actions.ExpireAsync(expiring.Id, createdAt.AddMinutes(11));
Assert(publisher.Events.Count == afterExpiredCount, "Expired replay must not re-emit Expired.");

var failurePublisher = new RecordingPublisher();
var failureDispatcher = new PaymentRequestEventDispatcher(failurePublisher);
var addFailRepo = new RecordingRepository { ThrowOnAdd = true };
var addFailService = new PaymentRequestApplicationService(addFailRepo, resolver, failureDispatcher);
await AssertThrowsAsync<InvalidOperationException>(
    () => addFailService.CreateAsync(new CreatePaymentRequestCommand(requester, recipient, currency, 1_000, Guid.NewGuid(), createdAt)),
    "Add failure must propagate.");
Assert(failurePublisher.Events.Count == 0, "Failed AddAsync must not emit Created.");

var updateFailRequest = PaymentRequest.Create(requester, recipient, currency, 1_000, Guid.NewGuid(), createdAt);
var updateFailRepo = new RecordingRepository();
await updateFailRepo.AddAsync(updateFailRequest);
updateFailRepo.ThrowOnUpdate = true;
var updateFailActions = new PaymentRequestActionService(updateFailRepo, resolver, ownership, payment, failureDispatcher);
await AssertThrowsAsync<InvalidOperationException>(
    () => updateFailActions.DeclineAsync(updateFailRequest.Id, actor, createdAt.AddMinutes(1)),
    "Update failure must propagate.");
Assert(failurePublisher.Events.Count == 0, "Failed UpdateAsync must not emit Declined.");

var paymentFailPublisher = new RecordingPublisher();
var paymentFailDispatcher = new PaymentRequestEventDispatcher(paymentFailPublisher);
var paymentFailRepo = new RecordingRepository();
var paymentFailRequest = PaymentRequest.Create(requester, recipient, currency, 1_000, Guid.NewGuid(), createdAt, createdAt.AddHours(1));
await paymentFailRepo.AddAsync(paymentFailRequest);
var paymentFailActions = new PaymentRequestActionService(
    paymentFailRepo,
    resolver,
    ownership,
    new ThrowingPaymentPort(),
    paymentFailDispatcher);
await AssertThrowsAsync<InvalidOperationException>(
    () => paymentFailActions.AcceptAndPayAsync(paymentFailRequest.Id, actor, createdAt.AddMinutes(1)),
    "Payment failure must propagate.");
Assert(paymentFailRequest.Status == PaymentRequestStatus.Accepted, "Acceptance must remain persisted before failed payment.");
Assert(paymentFailPublisher.Events.Count == 1 && paymentFailPublisher.Events[0].Kind == PaymentRequestEventKind.Accepted,
    "Failed payment must emit Accepted but not Paid.");

Console.WriteLine("AFW-BE-NOTIFICATION-1 payment request event emission integration scenarios: PASS");

sealed class RecordingPublisher : IPaymentRequestEventPublisher
{
    public List<PaymentRequestEvent> Events { get; } = [];

    public Task PublishAsync(PaymentRequestEvent paymentRequestEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Events.Add(paymentRequestEvent);
        return Task.CompletedTask;
    }
}

sealed class RecordingRepository : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> byId = [];
    private readonly Dictionary<Guid, PaymentRequest> byCorrelation = [];
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }
    public bool ThrowOnAdd { get; set; }
    public bool ThrowOnUpdate { get; set; }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
        if (ThrowOnAdd) throw new InvalidOperationException("Simulated add failure.");
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnUpdate) throw new InvalidOperationException("Simulated update failure.");
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

sealed class FixedRecipientResolver(WalletId walletId) : IPaymentRequestRecipientResolver
{
    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<WalletId?>(walletId);
    }
}

sealed class MultiOwnershipReader(IReadOnlyDictionary<Guid, Guid> owners) : IPaymentRequestWalletOwnershipReader
{
    public Task<bool> IsOwnedByAsync(WalletId walletId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(owners.TryGetValue(walletId.Value, out var expected) && expected == ownerId);
    }
}

sealed class RecordingPaymentPort : IPaymentRequestPaymentPort
{
    public PaymentRequestPaymentReceipt? LastReceipt { get; private set; }

    public Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastReceipt = new PaymentRequestPaymentReceipt(
            Guid.NewGuid(),
            sourceWalletId,
            targetWalletId,
            amountMinor,
            correlationId,
            requestedAtUtc);
        return Task.FromResult(LastReceipt);
    }
}

sealed class ThrowingPaymentPort : IPaymentRequestPaymentPort
{
    public Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated payment failure.");
}
