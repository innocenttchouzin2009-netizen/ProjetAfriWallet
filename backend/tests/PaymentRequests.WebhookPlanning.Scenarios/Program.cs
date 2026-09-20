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

var requesterWalletId = WalletId.From(Guid.NewGuid());
var payerReference = RecipientReference.FromAfWalId("recipient.one");
var createdAt = new DateTimeOffset(2026, 9, 20, 16, 0, 0, TimeSpan.Zero);
var request = PaymentRequest.Create(
    requesterWalletId,
    payerReference,
    Currency.Create("XAF"),
    5_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddHours(24));

var eventEnvelope = new PaymentRequestEventEnvelope(
    Guid.NewGuid(),
    request.Id,
    "payment-request.created",
    createdAt,
    "{\"paymentRequestId\":\"safe-id\",\"url\":\"https://attacker.invalid/not-authoritative\"}");

var destinationOne = new PaymentRequestWebhookDestination(
    Guid.NewGuid(),
    new Uri("https://hooks.partner-one.test/request-events"),
    new PaymentRequestWebhookRetryPolicy(4, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40)));
var destinationTwo = new PaymentRequestWebhookDestination(
    Guid.NewGuid(),
    new Uri("https://hooks.partner-two.test/request-events"),
    new PaymentRequestWebhookRetryPolicy(3, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)));

var repository = new FixedPaymentRequestRepository(request);
var registry = new RecordingDestinationRegistry([destinationOne, destinationTwo]);
var planner = new PaymentRequestWebhookDeliveryPlanner(repository, registry);

var result = await planner.PlanAsync(eventEnvelope);
Assert(result.Status == PaymentRequestWebhookDeliveryPlanningStatus.Planned, "Expected a planned result.");
Assert(result.Plan is not null, "Plan is required.");
Assert(result.Plan!.Destinations.Count == 2, "Expected fan-out to two authorized destinations.");
Assert(registry.Calls == 1, "Destination registry must be called exactly once.");
Assert(registry.LastRecipient == payerReference, "Planner must resolve destinations from the payment request payer identity.");
Assert(registry.LastEventType == eventEnvelope.EventType, "Event type must be forwarded to destination authorization.");

var first = result.Plan.Destinations.Single(item => item.DestinationId == destinationOne.DestinationId);
Assert(first.Endpoint == destinationOne.Endpoint, "Destination endpoint must come from registry.");
Assert(first.Endpoint.Host == "hooks.partner-one.test", "Business event payload URL must never become delivery endpoint.");
Assert(first.MaxAttempts == 4, "Retry max attempts must come from destination policy.");
Assert(first.RetryDelays.SequenceEqual([
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(20),
    TimeSpan.FromSeconds(40)
]), "Exponential retry schedule must be derived from destination policy.");

var second = result.Plan.Destinations.Single(item => item.DestinationId == destinationTwo.DestinationId);
Assert(second.RetryDelays.SequenceEqual([
    TimeSpan.FromMinutes(1),
    TimeSpan.FromMinutes(2)
]), "Per-destination retry policy must be preserved independently.");

var missing = await new PaymentRequestWebhookDeliveryPlanner(
    new FixedPaymentRequestRepository(null),
    registry).PlanAsync(eventEnvelope);
Assert(missing.Status == PaymentRequestWebhookDeliveryPlanningStatus.RequestNotFound, "Missing request must fail closed.");
Assert(registry.Calls == 1, "Registry must not be queried when request does not exist.");

var noDestinationsRegistry = new RecordingDestinationRegistry([]);
var noDestinations = await new PaymentRequestWebhookDeliveryPlanner(
    repository,
    noDestinationsRegistry).PlanAsync(eventEnvelope);
Assert(noDestinations.Status == PaymentRequestWebhookDeliveryPlanningStatus.NoAuthorizedDestinations,
    "No authorized destinations must be explicit.");

var duplicateId = Guid.NewGuid();
var duplicateRegistry = new RecordingDestinationRegistry([
    new PaymentRequestWebhookDestination(
        duplicateId,
        new Uri("https://hooks.one.test/request-events"),
        new PaymentRequestWebhookRetryPolicy(2, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))),
    new PaymentRequestWebhookDestination(
        duplicateId,
        new Uri("https://hooks.two.test/request-events"),
        new PaymentRequestWebhookRetryPolicy(2, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)))
]);
await AssertThrowsAsync<InvalidOperationException>(
    () => new PaymentRequestWebhookDeliveryPlanner(repository, duplicateRegistry).PlanAsync(eventEnvelope),
    "Duplicate destination ids must fail closed.");

var invalidEndpointRegistry = new RecordingDestinationRegistry([
    new PaymentRequestWebhookDestination(
        Guid.NewGuid(),
        new Uri("ftp://not-allowed.test/request-events"),
        new PaymentRequestWebhookRetryPolicy(2, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)))
]);
await AssertThrowsAsync<InvalidOperationException>(
    () => new PaymentRequestWebhookDeliveryPlanner(repository, invalidEndpointRegistry).PlanAsync(eventEnvelope),
    "Invalid registry endpoint must fail closed.");

var invalidRetryRegistry = new RecordingDestinationRegistry([
    new PaymentRequestWebhookDestination(
        Guid.NewGuid(),
        new Uri("https://hooks.test/request-events"),
        new PaymentRequestWebhookRetryPolicy(0, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)))
]);
await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => new PaymentRequestWebhookDeliveryPlanner(repository, invalidRetryRegistry).PlanAsync(eventEnvelope),
    "Invalid retry policy must fail closed.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => planner.PlanAsync(eventEnvelope, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-REQUEST webhook destination resolution and fan-out delivery planning scenarios: PASS");

sealed class FixedPaymentRequestRepository(PaymentRequest? value) : IPaymentRequestRepository
{
    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(value is not null && value.Id == id ? value : null);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PaymentRequest?>(null);

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

sealed class RecordingDestinationRegistry(IReadOnlyList<PaymentRequestWebhookDestination> destinations)
    : IPaymentRequestWebhookDestinationRegistry
{
    public int Calls { get; private set; }
    public RecipientReference? LastRecipient { get; private set; }
    public string? LastEventType { get; private set; }

    public Task<IReadOnlyList<PaymentRequestWebhookDestination>> ResolveAuthorizedAsync(
        RecipientReference recipient,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastRecipient = recipient;
        LastEventType = eventType;
        return Task.FromResult(destinations);
    }
}
