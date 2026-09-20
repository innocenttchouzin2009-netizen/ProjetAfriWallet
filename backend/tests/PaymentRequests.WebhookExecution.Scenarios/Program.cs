using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var createdAt = new DateTimeOffset(2026, 9, 20, 17, 0, 0, TimeSpan.Zero);
var recipient = RecipientReference.FromAfWalId("recipient.one");
var request = PaymentRequest.Create(
    WalletId.From(Guid.NewGuid()),
    recipient,
    Currency.Create("XAF"),
    5_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddHours(1));
var envelope = new PaymentRequestEventEnvelope(
    Guid.NewGuid(),
    request.Id,
    "payment-request.created",
    createdAt,
    "{\"endpoint\":\"https://attacker.invalid/never-use\"}");

var firstId = Guid.NewGuid();
var secondId = Guid.NewGuid();
var registry = new FixedRegistry([
    new PaymentRequestWebhookDestination(
        firstId,
        new Uri("https://one.test/webhooks"),
        new PaymentRequestWebhookRetryPolicy(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)),
        "signer-one"),
    new PaymentRequestWebhookDestination(
        secondId,
        new Uri("https://two.test/webhooks"),
        new PaymentRequestWebhookRetryPolicy(2, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(7)),
        "signer-two")
]);
var planner = new PaymentRequestWebhookDeliveryPlanner(new FixedRepository(request), registry);
var signerRegistry = new RecordingSignerRegistry();
var transport = new SequencedTransport(
    new Dictionary<string, Queue<PaymentRequestWebhookTransportResult>>(StringComparer.Ordinal)
    {
        ["one.test"] = new Queue<PaymentRequestWebhookTransportResult>([
            PaymentRequestWebhookTransportResult.TransientFailure(503),
            PaymentRequestWebhookTransportResult.Delivered(200)]),
        ["two.test"] = new Queue<PaymentRequestWebhookTransportResult>([
            PaymentRequestWebhookTransportResult.PermanentFailure(410)])
    });
var scheduler = new RecordingScheduler();
var executor = new PaymentRequestWebhookDeliveryExecutor(planner, signerRegistry, transport, scheduler);

var result = await executor.ExecuteAsync(envelope, createdAt.AddSeconds(1));

Assert(result.PlanningStatus == PaymentRequestWebhookDeliveryPlanningStatus.Planned, "Execution must be planned.");
Assert(result.Destinations.Count == 2, "Both authorized destinations must be executed.");
var first = result.Destinations.Single(item => item.DestinationId == firstId);
var second = result.Destinations.Single(item => item.DestinationId == secondId);
Assert(first.Outcome == PaymentRequestWebhookDeliveryOutcome.Delivered && first.Attempts == 2,
    "Transient destination must retry then succeed.");
Assert(second.Outcome == PaymentRequestWebhookDeliveryOutcome.PermanentFailure && second.Attempts == 1,
    "Permanent failure must not retry.");
Assert(scheduler.Delays.SequenceEqual([TimeSpan.FromSeconds(2)]),
    "Retry scheduler must use only the failing destination policy.");
Assert(signerRegistry.Resolutions.SequenceEqual([
    (firstId, "signer-one"),
    (secondId, "signer-two")
]), "Each destination must resolve its dedicated signer configuration.");
Assert(transport.Requests.All(request => request.Endpoint.Host is "one.test" or "two.test"),
    "Transport endpoints must come only from registry-backed plan.");
Assert(transport.Requests.All(request => request.Endpoint.Host != "attacker.invalid"),
    "Business event payload endpoint must never be used.");

Console.WriteLine("AFW-BE-REQUEST registry-backed webhook execution scenarios: PASS");

sealed class FixedRepository(PaymentRequest request) : IPaymentRequestRepository
{
    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<PaymentRequest?>(request.Id == id ? request : null);
    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PaymentRequest?>(null);
    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

sealed class FixedRegistry(IReadOnlyList<PaymentRequestWebhookDestination> destinations)
    : IPaymentRequestWebhookDestinationRegistry
{
    public Task<IReadOnlyList<PaymentRequestWebhookDestination>> ResolveAuthorizedAsync(
        RecipientReference recipient, string eventType, CancellationToken cancellationToken = default) =>
        Task.FromResult(destinations);
}

sealed class RecordingSignerRegistry : IPaymentRequestWebhookSignerRegistry
{
    public List<(Guid, string)> Resolutions { get; } = [];
    public Task<IPaymentRequestWebhookSigner> ResolveAsync(
        Guid destinationId, string signingConfigurationId, CancellationToken cancellationToken = default)
    {
        Resolutions.Add((destinationId, signingConfigurationId));
        return Task.FromResult<IPaymentRequestWebhookSigner>(new FixedSigner(signingConfigurationId));
    }
}

sealed class FixedSigner(string keyId) : IPaymentRequestWebhookSigner
{
    public Task<PaymentRequestWebhookSignature> SignAsync(
        PaymentRequestWebhookSignatureInput input, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentRequestWebhookSignature("hmac-sha256", "sig-" + keyId, keyId));
}

sealed class SequencedTransport(Dictionary<string, Queue<PaymentRequestWebhookTransportResult>> results)
    : IPaymentRequestWebhookTransport
{
    public List<PaymentRequestWebhookSignedRequest> Requests { get; } = [];
    public Task<PaymentRequestWebhookTransportResult> SendAsync(
        PaymentRequestWebhookSignedRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(results[request.Endpoint.Host].Dequeue());
    }
}

sealed class RecordingScheduler : IPaymentRequestWebhookRetryScheduler
{
    public List<TimeSpan> Delays { get; } = [];
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        Delays.Add(delay);
        return Task.CompletedTask;
    }
}
