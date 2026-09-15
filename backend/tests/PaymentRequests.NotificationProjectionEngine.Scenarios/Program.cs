using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Application;
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

var createdAt = new DateTimeOffset(2026, 9, 16, 0, 30, 0, TimeSpan.Zero);
var requesterOwner = Guid.NewGuid();
var payerOwner = Guid.NewGuid();
var requesterWallet = Wallet.Create(WalletId.From(Guid.NewGuid()), requesterOwner, Currency.Create("XAF"), null, createdAt);
var payerWallet = Wallet.Create(WalletId.From(Guid.NewGuid()), payerOwner, Currency.Create("XAF"), null, createdAt);
var payerReference = RecipientReference.FromAfWalId("payer.notify");
var request = PaymentRequest.Create(
    requesterWallet.Id,
    payerReference,
    Currency.Create("XAF"),
    25_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddDays(1));

var requestRepository = new InMemoryPaymentRequestRepository(request);
var walletRepository = new InMemoryWalletRepository([requesterWallet, payerWallet]);
var recipientResolver = new RecordingRecipientResolver(payerWallet.Id);
var audienceResolver = new PaymentRequestNotificationAudienceResolver(
    walletRepository,
    requestRepository,
    recipientResolver);
var store = new InMemoryProjectionStore();
var engine = new PaymentRequestNotificationProjectionEngine(audienceResolver, store);

var createdEvent = PaymentRequestLifecycleEventFactory.ToEnvelope(
    PaymentRequestLifecycleEventFactory.Create(request, PaymentRequestLifecycleEventKind.Created, createdAt));
var createdResult = await engine.ProjectAsync(createdEvent);
Assert(!createdResult.Ignored, "Created event must be projected.");
Assert(createdResult.ProjectedCount == 2 && createdResult.DuplicateCount == 0, "Created event must project requester and payer audiences.");
Assert(store.Notifications.Count == 2, "Two notifications are expected for created event.");
Assert(store.Notifications.Any(x => x.Audience == PaymentRequestNotificationAudience.Requester && x.RecipientOwnerId == requesterOwner), "Requester audience must resolve to requester owner.");
Assert(store.Notifications.Any(x => x.Audience == PaymentRequestNotificationAudience.Payer && x.RecipientOwnerId == payerOwner), "Payer audience must resolve to payer owner.");
Assert(recipientResolver.Calls == 1, "Created event must resolve payer from original payment request reference.");

var replay = await engine.ProjectAsync(createdEvent);
Assert(replay.ProjectedCount == 0 && replay.DuplicateCount == 2, "Replayed event must be idempotent through projection store.");
Assert(store.Notifications.Count == 2, "Replay must not create duplicate projections.");
Assert(recipientResolver.Calls == 2, "Replay may resolve audiences again before the projection store enforces idempotence.");

request.Accept(payerWallet.Id, createdAt.AddMinutes(5));
var acceptedEvent = PaymentRequestLifecycleEventFactory.ToEnvelope(
    PaymentRequestLifecycleEventFactory.Create(request, PaymentRequestLifecycleEventKind.Accepted, request.UpdatedAtUtc));
var acceptedResult = await engine.ProjectAsync(acceptedEvent);
Assert(acceptedResult.ProjectedCount == 2, "Accepted event must project both audiences.");
Assert(recipientResolver.Calls == 2, "Accepted event must use accepted payer wallet and avoid re-resolving public recipient reference.");

var ignored = await engine.ProjectAsync(new PaymentRequestEventEnvelope(
    Guid.NewGuid(),
    request.Id,
    "payment-request.unknown",
    createdAt,
    "{}"));
Assert(ignored.Ignored && ignored.ProjectedCount == 0, "Unknown event types must be ignored.");

await AssertThrowsAsync<InvalidOperationException>(
    () => engine.ProjectAsync(new PaymentRequestEventEnvelope(
        Guid.NewGuid(), request.Id, PaymentRequestNotificationEventTypes.Created, createdAt, "{")),
    "Malformed JSON payload must fail closed.");

await AssertThrowsAsync<InvalidOperationException>(
    () => engine.ProjectAsync(new PaymentRequestEventEnvelope(
        Guid.NewGuid(),
        request.Id,
        PaymentRequestNotificationEventTypes.Created,
        createdAt,
        "{\"version\":2,\"status\":\"Pending\",\"requesterWalletId\":\"" + requesterWallet.Id.Value + "\",\"currencyCode\":\"XAF\",\"amountMinor\":25000}")),
    "Unsupported payload version must fail closed.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => engine.ProjectAsync(createdEvent, cts.Token),
    "Projection cancellation must propagate.");

Console.WriteLine("AFW-BE-REQUEST-NOTIFY-1 lifecycle event notification projection scenarios: PASS");

sealed class InMemoryProjectionStore : IPaymentRequestNotificationProjectionStore
{
    private readonly HashSet<PaymentRequestNotificationProjectionKey> keys = [];
    public List<PaymentRequestNotification> Notifications { get; } = [];

    public Task<bool> TryAddAsync(PaymentRequestNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = new PaymentRequestNotificationProjectionKey(
            notification.SourceEventId,
            notification.RecipientOwnerId,
            notification.Audience);
        if (!keys.Add(key))
        {
            return Task.FromResult(false);
        }

        Notifications.Add(notification);
        return Task.FromResult(true);
    }
}

sealed class RecordingRecipientResolver(WalletId walletId) : IPaymentRequestRecipientResolver
{
    public int Calls { get; private set; }

    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult<WalletId?>(walletId);
    }
}

sealed class InMemoryPaymentRequestRepository(PaymentRequest request) : IPaymentRequestRepository
{
    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<PaymentRequest?>(id == request.Id ? request : null);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<PaymentRequest?>(correlationId == request.CorrelationId ? request : null);
    }

    public Task AddAsync(PaymentRequest value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UpdateAsync(PaymentRequest value, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(x => x.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(walletId.Value, out var wallet);
        return Task.FromResult(wallet);
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}
