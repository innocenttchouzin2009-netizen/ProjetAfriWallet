using System.Text.Json;
using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
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

var paymentSucceeded = WebhookEventType.From("payment.succeeded");
var transferFailed = WebhookEventType.From("transfer.failed");
var createdAt = new DateTimeOffset(2026, 9, 18, 16, 0, 0, TimeSpan.Zero);

var repository = new InMemoryRepository();
var subscriptionService = new PartnerWebhookSubscriptionApplicationService(repository);

var first = await subscriptionService.CreateAsync(new CreatePartnerWebhookSubscriptionCommand(
    PartnerId.From("partner.alpha"),
    WebhookEndpoint.From("https://alpha.example/webhooks"),
    WebhookSigningSecretReference.From("vault://partners/alpha/webhook-v1"),
    [paymentSucceeded],
    createdAt));

var second = await subscriptionService.CreateAsync(new CreatePartnerWebhookSubscriptionCommand(
    PartnerId.From("partner.beta"),
    WebhookEndpoint.From("https://beta.example/webhooks"),
    WebhookSigningSecretReference.From("vault://partners/beta/webhook-v3"),
    [paymentSucceeded, transferFailed],
    createdAt));

var suspended = await subscriptionService.CreateAsync(new CreatePartnerWebhookSubscriptionCommand(
    PartnerId.From("partner.suspended"),
    WebhookEndpoint.From("https://suspended.example/webhooks"),
    WebhookSigningSecretReference.From("vault://partners/suspended/webhook-v1"),
    [paymentSucceeded],
    createdAt));

await subscriptionService.SuspendAsync(
    suspended.Id,
    suspended.PartnerId,
    createdAt.AddMinutes(1));

var transferOnly = await subscriptionService.CreateAsync(new CreatePartnerWebhookSubscriptionCommand(
    PartnerId.From("partner.transfer-only"),
    WebhookEndpoint.From("https://transfer.example/webhooks"),
    WebhookSigningSecretReference.From("vault://partners/transfer/webhook-v1"),
    [transferFailed],
    createdAt));

using var payloadDocument = JsonDocument.Parse("""{"paymentRequestId":"req-1","amountMinor":2500}""");
var webhookEvent = OutboundWebhookEvent.Create(
    WebhookEventId.From(Guid.Parse("11111111-2222-3333-4444-555555555555")),
    paymentSucceeded,
    createdAt.AddMinutes(2),
    payloadDocument.RootElement);

var port = new RecordingDeliveryPort();
var orchestrator = new SubscriptionAwareWebhookDeliveryOrchestrator(subscriptionService, port);
var result = await orchestrator.DeliverAsync(webhookEvent);

Assert(result.EligibleSubscriptionCount == 2, "Exactly two active matching subscriptions must be eligible.");
Assert(result.DispatchedDeliveryCount == 2, "Exactly two deliveries must be dispatched.");
Assert(port.Deliveries.Count == 2, "Delivery port must be called once per eligible subscription.");

var expectedIds = new[] { first.Id, second.Id }
    .OrderBy(id => id.Value)
    .ToArray();

Assert(port.Deliveries[0].SubscriptionId == expectedIds[0], "Dispatch order must be deterministic by subscription id.");
Assert(port.Deliveries[1].SubscriptionId == expectedIds[1], "Dispatch order must be deterministic by subscription id.");
Assert(port.Deliveries.All(delivery => ReferenceEquals(delivery.Event, webhookEvent)), "The same outbound event instance must be forwarded.");
Assert(port.Deliveries.All(delivery => delivery.SigningSecretReference.Value.StartsWith("vault://", StringComparison.Ordinal)), "Routing must forward only secret references.");
Assert(port.Deliveries.All(delivery => delivery.Endpoint.Value.StartsWith("https://", StringComparison.Ordinal)), "Routing must forward certified HTTPS endpoints.");
Assert(port.Deliveries.All(delivery => delivery.SubscriptionId != suspended.Id), "Suspended subscriptions must not be routed.");
Assert(port.Deliveries.All(delivery => delivery.SubscriptionId != transferOnly.Id), "Non-matching subscriptions must not be routed.");

var noMatchEvent = OutboundWebhookEvent.Create(
    WebhookEventId.New(),
    WebhookEventType.From("refund.completed"),
    createdAt.AddMinutes(3),
    payloadDocument.RootElement);

var noMatchPort = new RecordingDeliveryPort();
var noMatchResult = await new SubscriptionAwareWebhookDeliveryOrchestrator(subscriptionService, noMatchPort)
    .DeliverAsync(noMatchEvent);

Assert(noMatchResult.EligibleSubscriptionCount == 0, "Unknown event type must have no eligible subscriptions.");
Assert(noMatchResult.DispatchedDeliveryCount == 0, "No eligible subscriptions means no dispatches.");
Assert(noMatchPort.Deliveries.Count == 0, "Delivery port must not be called when no subscription matches.");

var failingPort = new RecordingDeliveryPort(failOnCall: 1);
var failingOrchestrator = new SubscriptionAwareWebhookDeliveryOrchestrator(subscriptionService, failingPort);

await AssertThrowsAsync<InvalidOperationException>(
    () => failingOrchestrator.DeliverAsync(webhookEvent),
    "Delivery port failure must propagate fail-fast.");

Assert(failingPort.Calls == 1, "Fail-fast orchestration must stop after the first delivery failure.");

using var cts = new CancellationTokenSource();
cts.Cancel();

await AssertThrowsAsync<OperationCanceledException>(
    () => orchestrator.DeliverAsync(webhookEvent, cts.Token),
    "Cancellation must propagate before subscription routing.");

Console.WriteLine("AFW-BE-WEBHOOK-ROUTING-1 subscription-aware delivery orchestration scenarios: PASS");

sealed class RecordingDeliveryPort(int? failOnCall = null) : ISubscriptionWebhookDeliveryPort
{
    public List<SubscriptionWebhookDelivery> Deliveries { get; } = [];
    public int Calls { get; private set; }

    public Task DeliverAsync(
        SubscriptionWebhookDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;

        if (failOnCall == Calls)
        {
            throw new InvalidOperationException("Synthetic delivery failure.");
        }

        Deliveries.Add(delivery);
        return Task.CompletedTask;
    }
}

sealed class InMemoryRepository : IPartnerWebhookSubscriptionRepository
{
    private readonly Dictionary<Guid, PartnerWebhookSubscription> values = new();

    public Task<PartnerWebhookSubscription?> GetAsync(
        PartnerWebhookSubscriptionId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var subscription);
        return Task.FromResult(subscription);
    }

    public Task AddAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(subscription.Id.Value, subscription);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!values.ContainsKey(subscription.Id.Value))
        {
            throw new InvalidOperationException("Webhook subscription was not found.");
        }

        values[subscription.Id.Value] = subscription;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PartnerWebhookSubscription>> ListActiveByEventTypeAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<PartnerWebhookSubscription> result = values.Values
            .Where(subscription => subscription.Accepts(eventType))
            .ToArray();

        return Task.FromResult(result);
    }
}
