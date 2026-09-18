using System.Net;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 18, 18, 0, 0, TimeSpan.Zero);
var timeProvider = new FixedTimeProvider(now);
var secretProvider = new RotatingPaymentRequestWebhookSecretProvider(
    new PaymentRequestWebhookSecret(
        "key-2026-09",
        "0123456789abcdef0123456789abcdef",
        DateTimeOffset.UnixEpoch));
var signer = new PaymentRequestWebhookSigner(secretProvider);
var endpoint = new Uri("https://receiver.example.test/hooks/payment-requests");

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var dbOptions = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(connection)
    .Options;
await using var db = new PaymentRequestDbContext(dbOptions);
await db.Database.EnsureCreatedAsync();

var store = new EfPaymentRequestEventOutboxStore(db);
var ledger = new EfPaymentRequestEventAttemptLedger(db);
var finalizer = new EfPaymentRequestEventAttemptFinalizer(db);
var deliveryOptions = new PaymentRequestEventDeliveryOptions(
    3,
    TimeSpan.FromMinutes(5),
    TimeSpan.FromSeconds(30));

var successHandler = new RecordingHandler(HttpStatusCode.Accepted);
var successTransport = new HttpPaymentRequestEventTransport(
    new HttpClient(successHandler),
    signer,
    new PaymentRequestWebhookHttpDeliveryOptions(endpoint),
    timeProvider);
var successProcessor = new PaymentRequestEventOutboxProcessor(
    store,
    new ProviderNeutralPaymentRequestEventDeliveryAdapter(successTransport),
    ledger,
    finalizer,
    deliveryOptions);

var eventId = Guid.NewGuid();
var paymentRequestId = PaymentRequestId.From(Guid.NewGuid());
var payload = $$"""{"paymentRequestId":"{{paymentRequestId.Value:D}}","status":"Paid"}""";
var envelope = new PaymentRequestEventEnvelope(
    eventId,
    paymentRequestId,
    "payment-request.paid",
    now.AddMinutes(-1),
    payload);

Assert(await store.EnqueueAsync(envelope, now), "Webhook event must be enqueued.");
var delivered = await successProcessor.ProcessBatchAsync(10, now);
Assert(delivered == 1, "Successful HTTP delivery must count as delivered.");

var successAttempts = await ledger.ListByEventAsync(eventId);
Assert(successAttempts.Count == 1, "Successful delivery must create exactly one attempt ledger row.");
Assert(
    successAttempts[0].Outcome == PaymentRequestEventAttemptOutcome.Delivered,
    "Successful HTTP delivery must finalize attempt as Delivered.");

var deliveredRow = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == eventId);
Assert(
    deliveredRow.Status == (int)PaymentRequestEventOutboxStatus.Delivered,
    "Successful HTTP delivery must atomically finalize Outbox as Delivered.");
Assert(successHandler.LastBody == payload, "HTTP transport must preserve exact JSON body bytes.");
Assert(successHandler.Method == HttpMethod.Post, "Webhook transport must use POST.");
Assert(successHandler.RequestUri == endpoint, "Webhook endpoint mismatch.");
Assert(
    successHandler.Header(PaymentRequestWebhookHeaderNames.EventId) == eventId.ToString("D"),
    "Webhook event id header mismatch.");
Assert(
    successHandler.Header(PaymentRequestWebhookHeaderNames.KeyId) == "key-2026-09",
    "Webhook key id header mismatch.");
Assert(
    successHandler.Header(PaymentRequestWebhookHeaderNames.Timestamp) ==
    now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
    "Webhook signature timestamp mismatch.");
var signature = successHandler.Header(PaymentRequestWebhookHeaderNames.Signature);
Assert(
    signature.StartsWith("v1=", StringComparison.Ordinal) && signature.Length == 67,
    "Webhook signature must be v1 HMAC-SHA256 hex.");

var retryAt = now.AddMinutes(1);
timeProvider.SetUtcNow(retryAt);
var transientEventId = Guid.NewGuid();
Assert(
    await store.EnqueueAsync(
        new PaymentRequestEventEnvelope(
            transientEventId,
            PaymentRequestId.From(Guid.NewGuid()),
            "payment-request.accepted",
            retryAt,
            """{"status":"Accepted"}"""),
        retryAt),
    "Transient webhook event must be enqueued.");

var transientTransport = new HttpPaymentRequestEventTransport(
    new HttpClient(new RecordingHandler(HttpStatusCode.ServiceUnavailable)),
    signer,
    new PaymentRequestWebhookHttpDeliveryOptions(endpoint),
    timeProvider);
var transientProcessor = new PaymentRequestEventOutboxProcessor(
    store,
    new ProviderNeutralPaymentRequestEventDeliveryAdapter(transientTransport),
    ledger,
    finalizer,
    deliveryOptions);

var transientDelivered = await transientProcessor.ProcessBatchAsync(10, retryAt);
Assert(transientDelivered == 0, "HTTP 5xx must not count as delivered.");

var transientAttempt = (await ledger.ListByEventAsync(transientEventId)).Single();
Assert(
    transientAttempt.Outcome == PaymentRequestEventAttemptOutcome.RetryScheduled,
    "HTTP 5xx must finalize Attempt Ledger as RetryScheduled.");
Assert(
    transientAttempt.NextAttemptAtUtc == retryAt.AddSeconds(30),
    "Retry timestamp must be recorded in Attempt Ledger.");

var retryRow = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == transientEventId);
Assert(
    retryRow.Status == (int)PaymentRequestEventOutboxStatus.Retry,
    "HTTP 5xx must atomically return Outbox to Retry.");
Assert(
    retryRow.AvailableAtUtc == retryAt.AddSeconds(30).UtcDateTime,
    "Outbox retry eligibility must match Attempt Ledger.");

Console.WriteLine(
    "AFW-BE-REQUEST-WEBHOOK-REINTEGRATION-1 Outbox + Attempt Ledger + HMAC + HTTP scenarios: PASS");

sealed class FixedTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private DateTimeOffset current = initial;
    public override DateTimeOffset GetUtcNow() => current;
    public void SetUtcNow(DateTimeOffset value) => current = value;
}

sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    private readonly Dictionary<string, string> headers =
        new(StringComparer.OrdinalIgnoreCase);

    public HttpMethod? Method { get; private set; }
    public Uri? RequestUri { get; private set; }
    public string? LastBody { get; private set; }

    public string Header(string name) =>
        headers.TryGetValue(name, out var value)
            ? value
            : throw new InvalidOperationException($"Missing header {name}.");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Method = request.Method;
        RequestUri = request.RequestUri;
        LastBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        foreach (var pair in request.Headers)
            headers[pair.Key] = pair.Value.Single();

        return new HttpResponseMessage(statusCode);
    }
}
