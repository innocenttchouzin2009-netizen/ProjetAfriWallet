using System.Net;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;
using AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task DispatchIgnoringFailureAsync(
    RegistryBackedHttpPaymentRequestEventTransport transport,
    PaymentRequestEventDispatch dispatch)
{
    try
    {
        await transport.DispatchAsync(dispatch);
    }
    catch (PaymentRequestEventTransportException)
    {
    }
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-webhook-reliability-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<PaymentRequestWebhookSubscriptionDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

const string secretReference = "AFW_TEST_WEBHOOK_RELIABILITY_SECRET";
const string rawSecret = "0123456789abcdef0123456789abcdef";

try
{
    Environment.SetEnvironmentVariable(secretReference, rawSecret);

    await using var db = new PaymentRequestWebhookSubscriptionDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var registry = new EfPaymentRequestWebhookSubscriptionRegistry(db);
    var attempts = new EfPaymentRequestWebhookDeliveryAttemptStore(db);
    var now = new DateTimeOffset(2026, 9, 18, 17, 0, 0, TimeSpan.Zero);
    var subscription = PaymentRequestWebhookSubscription.Create(
        "integration.reliability",
        null,
        new Uri("https://reliability.example.test/hooks"),
        "reliability-key",
        secretReference,
        ["payment-request.paid"],
        now);
    await registry.AddAsync(subscription);

    var handler = new SequenceHandler(
        HttpStatusCode.Accepted,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.BadRequest,
        null);

    var transport = new RegistryBackedHttpPaymentRequestEventTransport(
        new HttpClient(handler),
        registry,
        new EnvironmentPaymentRequestWebhookSigningSecretResolver(),
        attempts,
        TimeProvider.System);

    var eventIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
    for (var index = 0; index < eventIds.Length; index++)
    {
        var dispatch = new PaymentRequestEventDispatch(
            eventIds[index],
            Guid.NewGuid(),
            "payment-request.paid",
            now.AddSeconds(index),
            $$"""{"paymentRequestId":"{{Guid.NewGuid():D}}","status":"Paid","sequence":{{index}}}""");
        await DispatchIgnoringFailureAsync(transport, dispatch);
    }

    var ledger = await attempts.ListAsync(subscription.Id);
    Assert(ledger.Count == 4, "Every actual HTTP delivery attempt must be recorded.");
    Assert(ledger.Select(x => x.EventId).ToHashSet().SetEquals(eventIds), "Attempt ledger event ids mismatch.");
    Assert(ledger.Count(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.Success) == 1, "Expected one successful attempt.");
    Assert(ledger.Count(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure) == 2, "Expected two transient failures.");
    Assert(ledger.Count(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure) == 1, "Expected one permanent failure.");
    Assert(ledger.Single(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.Success).HttpStatusCode == 202, "Success HTTP status mismatch.");
    Assert(ledger.Single(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure).HttpStatusCode == 400, "Permanent HTTP status mismatch.");
    Assert(ledger.Any(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure && x.HttpStatusCode == 503), "Transient HTTP status missing.");
    Assert(ledger.Any(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure && x.HttpStatusCode is null), "Network failure must persist without an HTTP status.");
    Assert(ledger.All(x => x.LatencyMilliseconds >= 0), "Latency must be non-negative.");

    var metrics = await attempts.GetMetricsAsync(subscription.Id);
    Assert(metrics.AttemptCount == 4, "Attempt count mismatch.");
    Assert(metrics.SuccessfulAttemptCount == 1, "Success count mismatch.");
    Assert(metrics.TransientFailureCount == 2, "Transient failure count mismatch.");
    Assert(metrics.PermanentFailureCount == 1, "Permanent failure count mismatch.");
    Assert(metrics.FailureRate == 0.75m, "Failure rate mismatch.");
    Assert(metrics.LastSuccessfulDeliveryAtUtc is not null, "Last successful delivery timestamp is required.");
    Assert(metrics.LastAttemptAtUtc is not null, "Last attempt timestamp is required.");
    Assert(metrics.AverageLatencyMilliseconds is >= 0d, "Average latency is required.");

    var properties = db.Model
        .FindEntityType(typeof(PaymentRequestWebhookDeliveryAttemptEntity))!
        .GetProperties()
        .Select(x => x.Name)
        .ToArray();
    Assert(!properties.Any(x =>
        x.Contains("Payload", StringComparison.OrdinalIgnoreCase) ||
        x.Contains("Body", StringComparison.OrdinalIgnoreCase) ||
        x.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        x.Contains("Signature", StringComparison.OrdinalIgnoreCase)),
        "Delivery attempt persistence must not contain payload, body, secret or signature fields.");

    var empty = await attempts.GetMetricsAsync(Guid.NewGuid());
    Assert(empty.AttemptCount == 0 && empty.FailureRate == 0m, "Unknown subscription metrics must be empty.");

    Console.WriteLine("AFW-BE-REQUEST Commit 12 webhook delivery attempt ledger and reliability metrics scenarios: PASS");
}
finally
{
    Environment.SetEnvironmentVariable(secretReference, null);
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class SequenceHandler(params HttpStatusCode?[] outcomes) : HttpMessageHandler
{
    private readonly Queue<HttpStatusCode?> values = new(outcomes);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0)
            throw new InvalidOperationException("No configured delivery outcome remains.");

        var status = values.Dequeue();
        if (status is null)
            throw new HttpRequestException("Simulated network failure.");

        return Task.FromResult(new HttpResponseMessage(status.Value));
    }
}
