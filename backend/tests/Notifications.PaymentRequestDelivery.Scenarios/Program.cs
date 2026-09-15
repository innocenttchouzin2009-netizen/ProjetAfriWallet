using AfriWallet.Notifications.Application;
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

var requesterUserId = Guid.NewGuid();
var payerUserId = Guid.NewGuid();
var requesterWalletId = WalletId.From(Guid.NewGuid());
var payerWalletId = WalletId.From(Guid.NewGuid());
var createdAt = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.Zero);
var request = PaymentRequest.Create(
    requesterWalletId,
    RecipientReference.FromAfWalId("payer.afwal"),
    Currency.Create("EUR"),
    2500,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddHours(1));

var repository = new InMemoryPaymentRequestRepository(request);
var payerResolver = new FixedPayerResolver(payerWalletId);
var wallets = new InMemoryWalletRepository([
    Wallet.Create(requesterWalletId, requesterUserId, Currency.Create("EUR"), null, createdAt),
    Wallet.Create(payerWalletId, payerUserId, Currency.Create("EUR"), null, createdAt)
]);
var recipientResolver = new PaymentRequestNotificationRecipientResolver(repository, payerResolver, wallets);
var delivery = new RecordingDeliveryPort();
var consumer = new PaymentRequestEventNotificationConsumer(recipientResolver, delivery);

var createdEvent = new PaymentRequestEventEnvelope(
    Guid.NewGuid(),
    request.Id,
    "payment-request.created",
    createdAt,
    "{}");
await consumer.ConsumeAsync(createdEvent);
Assert(delivery.Notifications.Count == 1, "Created event must produce one notification.");
Assert(delivery.Notifications[0].RecipientUserId == payerUserId, "Created event must target payer owner.");
Assert(delivery.Notifications[0].Channel == NotificationChannel.InApp, "Only In-App channel is allowed.");
Assert(delivery.Notifications[0].NotificationId == createdEvent.EventId, "Event id must be the stable notification id.");
Assert(delivery.Notifications[0].AmountMinor == 2500 && delivery.Notifications[0].CurrencyCode == "EUR", "Payment request amount context mismatch.");

var cancelledEvent = new PaymentRequestEventEnvelope(Guid.NewGuid(), request.Id, "payment-request.cancelled", createdAt.AddMinutes(1), "{}");
await consumer.ConsumeAsync(cancelledEvent);
Assert(delivery.Notifications[^1].RecipientUserId == payerUserId, "Cancelled event must target payer owner.");

foreach (var eventType in new[] { "payment-request.accepted", "payment-request.declined", "payment-request.expired", "payment-request.paid" })
{
    var envelope = new PaymentRequestEventEnvelope(Guid.NewGuid(), request.Id, eventType, createdAt.AddMinutes(2), "{}");
    await consumer.ConsumeAsync(envelope);
    Assert(delivery.Notifications[^1].RecipientUserId == requesterUserId, $"{eventType} must target requester owner.");
}

var transport = new InAppPaymentRequestEventTransport(consumer);
var transportEventId = Guid.NewGuid();
await transport.DispatchAsync(new PaymentRequestEventDispatch(
    transportEventId,
    request.Id.Value,
    "payment-request.created",
    createdAt.AddMinutes(3),
    "{\"version\":1}"));
Assert(delivery.Notifications[^1].SourceEventId == transportEventId, "Transport must preserve event identity.");
Assert(delivery.Notifications[^1].RecipientUserId == payerUserId, "Transport must preserve recipient resolution semantics.");

var beforeUnsupported = delivery.Notifications.Count;
await AssertThrowsAsync<NotSupportedException>(
    () => consumer.ConsumeAsync(new PaymentRequestEventEnvelope(Guid.NewGuid(), request.Id, "payment-request.unknown", createdAt, "{}")),
    "Unknown event type must fail closed.");
Assert(delivery.Notifications.Count == beforeUnsupported, "Unsupported event must not be delivered.");

var missingRepository = new InMemoryPaymentRequestRepository();
var missingResolver = new PaymentRequestNotificationRecipientResolver(missingRepository, payerResolver, wallets);
var missingConsumer = new PaymentRequestEventNotificationConsumer(missingResolver, new RecordingDeliveryPort());
await AssertThrowsAsync<InvalidOperationException>(
    () => missingConsumer.ConsumeAsync(new PaymentRequestEventEnvelope(Guid.NewGuid(), PaymentRequestId.From(Guid.NewGuid()), "payment-request.created", createdAt, "{}")),
    "Missing payment request must fail closed.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => consumer.ConsumeAsync(createdEvent, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW payment request event to In-App notification mapping scenarios: PASS");

sealed class RecordingDeliveryPort : INotificationDeliveryPort
{
    public List<InAppNotification> Notifications { get; } = [];

    public Task DeliverAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Notifications.Add(notification);
        return Task.CompletedTask;
    }
}

sealed class FixedPayerResolver(WalletId payerWalletId) : IPaymentRequestRecipientResolver
{
    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<WalletId?>(payerWalletId);
    }
}

sealed class InMemoryPaymentRequestRepository(params PaymentRequest[] requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> values = requests.ToDictionary(x => x.Id.Value);

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var request);
        return Task.FromResult(request);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.Values.SingleOrDefault(x => x.CorrelationId == correlationId));
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
        return Task.CompletedTask;
    }
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
