using System.Net;
using System.Text.Json;
using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;
using AfriWallet.PartnerWebhooks.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<PartnerWebhookDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new PartnerWebhookDbContext(options);
await db.Database.EnsureCreatedAsync();

var repository = new EfWebhookDeliveryAttemptRepository(db);
var subscriptionId = PartnerWebhookSubscriptionId.New();
var eventId = WebhookEventId.New();
using var document = JsonDocument.Parse("{\"payment_request_id\":\"abc\"}");
var webhookEvent = OutboundWebhookEvent.Create(
    eventId,
    WebhookEventType.Create("payment_request.paid"),
    new DateTimeOffset(2026, 9, 18, 16, 0, 0, TimeSpan.Zero),
    document.RootElement);

var delivery = new SubscriptionWebhookDelivery(
    subscriptionId,
    PartnerId.Create("partner-one"),
    WebhookEndpoint.Create("https://partner.example/webhooks"),
    WebhookSigningSecretReference.Create("secret-ref"),
    webhookEvent);

var startedAt = new DateTimeOffset(2026, 9, 18, 16, 1, 0, TimeSpan.Zero);
var successPort = new SequenceDeliveryPort();
var service = new ReliableSubscriptionWebhookDeliveryService(
    successPort,
    repository,
    new WebhookRetryBackoffPolicy());

var first = await service.DeliverOnceAsync(delivery, startedAt);
Assert(first.Delivered && !first.AlreadyDelivered, "First successful delivery must be recorded.");
Assert(first.Attempt?.AttemptNumber == 1, "First attempt number must be 1.");
Assert(await repository.CountAsync(subscriptionId, eventId) == 1, "One delivery attempt must be persisted.");

var duplicate = await service.DeliverOnceAsync(delivery, startedAt.AddMinutes(1));
Assert(duplicate.Delivered && duplicate.AlreadyDelivered, "Successful delivery must be idempotent.");
Assert(successPort.Calls == 1, "Idempotent replay must not call transport again.");
Assert(await repository.CountAsync(subscriptionId, eventId) == 1, "Idempotent replay must not create another attempt.");

var transientSubscription = PartnerWebhookSubscriptionId.New();
var transientDelivery = delivery with { SubscriptionId = transientSubscription };
var transientPort = new SequenceDeliveryPort(
    new HttpRequestException("server error", null, HttpStatusCode.InternalServerError));
var transientService = new ReliableSubscriptionWebhookDeliveryService(
    transientPort,
    repository,
    new WebhookRetryBackoffPolicy(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)));

var transient = await transientService.DeliverOnceAsync(transientDelivery, startedAt);
Assert(!transient.Delivered, "HTTP 500 must fail current attempt.");
Assert(transient.Attempt?.Outcome == WebhookDeliveryAttemptOutcome.TransientFailure, "HTTP 500 must be transient.");
Assert(transient.Attempt?.HttpStatusCode == 500, "HTTP status must be recorded.");
Assert(transient.Attempt?.NextRetryAtUtc is not null, "Transient failure must schedule retry eligibility.");

var permanentSubscription = PartnerWebhookSubscriptionId.New();
var permanentDelivery = delivery with { SubscriptionId = permanentSubscription };
var permanentPort = new SequenceDeliveryPort(
    new HttpRequestException("bad request", null, HttpStatusCode.BadRequest));
var permanentService = new ReliableSubscriptionWebhookDeliveryService(
    permanentPort,
    repository,
    new WebhookRetryBackoffPolicy());

var permanent = await permanentService.DeliverOnceAsync(permanentDelivery, startedAt);
Assert(permanent.Attempt?.Outcome == WebhookDeliveryAttemptOutcome.PermanentFailure, "HTTP 400 must be permanent.");
Assert(permanent.Attempt?.NextRetryAtUtc is null, "Permanent failure must not prepare retry.");

Assert(
    WebhookDeliveryFailureClassifier.Classify(
        new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests)) ==
    WebhookDeliveryFailureKind.Transient,
    "HTTP 429 must be transient.");

Assert(
    WebhookDeliveryFailureClassifier.Classify(new HttpRequestException("network")) ==
    WebhookDeliveryFailureKind.Transient,
    "Network failures without HTTP status must be transient.");

var backoff = new WebhookRetryBackoffPolicy(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
Assert(backoff.GetDelay(1) == TimeSpan.FromSeconds(5), "Attempt 1 backoff mismatch.");
Assert(backoff.GetDelay(2) == TimeSpan.FromSeconds(10), "Attempt 2 backoff mismatch.");
Assert(backoff.GetDelay(3) == TimeSpan.FromSeconds(20), "Attempt 3 backoff mismatch.");
Assert(backoff.GetDelay(4) == TimeSpan.FromSeconds(20), "Backoff must be capped.");

Console.WriteLine("AFW-BE-WEBHOOK-ROUTING-1 delivery reliability scenarios: PASS");

sealed class SequenceDeliveryPort(params Exception[] failures) : ISubscriptionWebhookDeliveryPort
{
    private readonly Queue<Exception> failures = new(failures);
    public int Calls { get; private set; }

    public Task DeliverAsync(
        SubscriptionWebhookDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;

        if (failures.Count > 0)
        {
            throw failures.Dequeue();
        }

        return Task.CompletedTask;
    }
}
