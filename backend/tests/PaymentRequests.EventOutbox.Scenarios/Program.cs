using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new PaymentRequestDbContext(options);
await db.Database.EnsureCreatedAsync();
var store = new EfPaymentRequestEventOutboxStore(db);

var now = new DateTimeOffset(2026, 9, 14, 20, 0, 0, TimeSpan.Zero);
var eventId = Guid.NewGuid();
var requestId = PaymentRequestId.From(Guid.NewGuid());
var envelope = new PaymentRequestEventEnvelope(
    eventId,
    requestId,
    "payment-request.created",
    now,
    "{\"version\":1}");

Assert(await store.EnqueueAsync(envelope, now), "First enqueue must succeed.");
Assert(!await store.EnqueueAsync(envelope, now), "Duplicate EventId must be deduplicated.");
Assert(await db.PaymentRequestEventOutbox.CountAsync() == 1, "Duplicate enqueue must not create a second row.");

var claimed = await store.ClaimBatchAsync(10, now, TimeSpan.FromMinutes(5));
Assert(claimed.Count == 1, "Pending event must be claimable.");
Assert(claimed[0].Status == PaymentRequestEventOutboxStatus.Processing, "Claimed event must be Processing.");
Assert(claimed[0].AttemptCount == 1, "First claim must increment attempt count.");

await store.MarkFailedAsync(eventId, now, "temporary failure", now.AddSeconds(30), false);
var tooEarly = await store.ClaimBatchAsync(10, now.AddSeconds(29), TimeSpan.FromMinutes(5));
Assert(tooEarly.Count == 0, "Retry event must not be visible before AvailableAtUtc.");
var retry = await store.ClaimBatchAsync(10, now.AddSeconds(30), TimeSpan.FromMinutes(5));
Assert(retry.Count == 1 && retry[0].AttemptCount == 2, "Retry must be claimed and increment attempts.");
await store.MarkDeliveredAsync(eventId, now.AddSeconds(30));
Assert((await db.PaymentRequestEventOutbox.AsNoTracking().SingleAsync(x => x.EventId == eventId)).Status ==
       (int)PaymentRequestEventOutboxStatus.Delivered,
    "Delivered event must be terminal.");

var crashedId = Guid.NewGuid();
var crashed = new PaymentRequestEventEnvelope(crashedId, requestId, "payment-request.accepted", now, "{}");
Assert(await store.EnqueueAsync(crashed, now), "Crash recovery event enqueue must succeed.");
var crashClaim = await store.ClaimBatchAsync(1, now, TimeSpan.FromMinutes(1));
Assert(crashClaim.Count == 1 && crashClaim[0].Event.EventId == crashedId, "Crash event must be claimed.");
Assert(await store.RecoverExpiredClaimsAsync(now.AddMinutes(1)) == 1, "Expired processing lease must be recovered.");
var recovered = await store.ClaimBatchAsync(1, now.AddMinutes(1), TimeSpan.FromMinutes(1));
Assert(recovered.Count == 1 && recovered[0].Event.EventId == crashedId, "Recovered event must be claimable after restart.");

var delivery = new FlakyDeliveryPort(failuresBeforeSuccess: 1);
var processor = new PaymentRequestEventOutboxProcessor(
    store,
    delivery,
    new PaymentRequestEventDeliveryOptions(4, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10)));
await store.MarkFailedAsync(crashedId, now.AddMinutes(1), "release for processor", now.AddMinutes(1), false);
var firstPass = await processor.ProcessBatchAsync(10, now.AddMinutes(1));
Assert(firstPass == 0, "First delivery attempt must fail and schedule retry.");
var beforeBackoff = await processor.ProcessBatchAsync(10, now.AddMinutes(1).AddSeconds(39));
Assert(beforeBackoff == 0, "Exponential backoff must prevent early retry delivery.");
var secondPass = await processor.ProcessBatchAsync(10, now.AddMinutes(1).AddSeconds(40));
Assert(secondPass == 1, "Delivery must succeed when the exponential retry window opens.");

var deadId = Guid.NewGuid();
Assert(await store.EnqueueAsync(new PaymentRequestEventEnvelope(deadId, requestId, "payment-request.declined", now, "{}"), now),
    "Dead-letter event enqueue must succeed.");
var alwaysFail = new PaymentRequestEventOutboxProcessor(
    store,
    new FlakyDeliveryPort(int.MaxValue),
    new PaymentRequestEventDeliveryOptions(2, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1)));
await alwaysFail.ProcessBatchAsync(10, now.AddHours(1));
await alwaysFail.ProcessBatchAsync(10, now.AddHours(1).AddSeconds(1));
var dead = await db.PaymentRequestEventOutbox.AsNoTracking().SingleAsync(x => x.EventId == deadId);
Assert(dead.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter, "Event must dead-letter after max attempts.");

var dispatchEventId = Guid.NewGuid();
var dispatchAt = now.AddHours(2);
var dispatchEnvelope = new PaymentRequestEventEnvelope(
    dispatchEventId,
    requestId,
    "payment-request.paid",
    dispatchAt,
    "{\"amountMinor\":2500}");
Assert(await store.EnqueueAsync(dispatchEnvelope, dispatchAt), "Provider-neutral dispatch event enqueue must succeed.");
var capturingTransport = new CapturingTransport();
var dispatchProcessor = new PaymentRequestEventOutboxProcessor(
    store,
    new ProviderNeutralPaymentRequestEventDeliveryAdapter(capturingTransport),
    new PaymentRequestEventDeliveryOptions(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5)));
Assert(await dispatchProcessor.ProcessBatchAsync(10, dispatchAt) == 1, "Provider-neutral transport success must mark delivery.");
Assert(capturingTransport.LastDispatch is not null, "Transport must receive a dispatch envelope.");
Assert(capturingTransport.LastDispatch!.EventId == dispatchEventId, "Transport event id mismatch.");
Assert(capturingTransport.LastDispatch.PaymentRequestId == requestId.Value, "Transport payment request id mismatch.");
Assert(capturingTransport.LastDispatch.EventType == "payment-request.paid", "Transport event type mismatch.");
Assert(capturingTransport.LastDispatch.PayloadJson == "{\"amountMinor\":2500}", "Transport payload mismatch.");

var transientId = Guid.NewGuid();
var transientAt = now.AddHours(3);
Assert(await store.EnqueueAsync(
    new PaymentRequestEventEnvelope(transientId, requestId, "payment-request.accepted", transientAt, "{}"),
    transientAt), "Transient event enqueue must succeed.");
var transientProcessor = new PaymentRequestEventOutboxProcessor(
    store,
    new ProviderNeutralPaymentRequestEventDeliveryAdapter(
        new FailingTransport(PaymentRequestEventTransportFailureKind.Transient)),
    new PaymentRequestEventDeliveryOptions(5, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5)));
Assert(await transientProcessor.ProcessBatchAsync(10, transientAt) == 0, "Transient transport failure must not count as delivered.");
var transientRow = await db.PaymentRequestEventOutbox.AsNoTracking().SingleAsync(x => x.EventId == transientId);
Assert(transientRow.Status == (int)PaymentRequestEventOutboxStatus.Retry, "Transient transport failure must map to Retry.");
Assert(transientRow.AttemptCount == 1, "Transient transport failure must consume one attempt.");

var permanentId = Guid.NewGuid();
var permanentAt = now.AddHours(4);
Assert(await store.EnqueueAsync(
    new PaymentRequestEventEnvelope(permanentId, requestId, "payment-request.cancelled", permanentAt, "{}"),
    permanentAt), "Permanent event enqueue must succeed.");
var permanentProcessor = new PaymentRequestEventOutboxProcessor(
    store,
    new ProviderNeutralPaymentRequestEventDeliveryAdapter(
        new FailingTransport(PaymentRequestEventTransportFailureKind.Permanent)),
    new PaymentRequestEventDeliveryOptions(5, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5)));
Assert(await permanentProcessor.ProcessBatchAsync(10, permanentAt) == 0, "Permanent transport failure must not count as delivered.");
var permanentRow = await db.PaymentRequestEventOutbox.AsNoTracking().SingleAsync(x => x.EventId == permanentId);
Assert(permanentRow.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter,
    "Permanent transport failure must dead-letter immediately.");
Assert(permanentRow.AttemptCount == 1, "Permanent transport failure must dead-letter on the first attempt.");

Console.WriteLine("AFW-BE-REQUEST-EVENTS-1 durable event outbox and provider-neutral transport scenarios: PASS");

sealed class FlakyDeliveryPort(int failuresBeforeSuccess) : IPaymentRequestEventDeliveryPort
{
    private int calls;

    public Task DeliverAsync(PaymentRequestEventEnvelope paymentRequestEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        calls++;
        if (calls <= failuresBeforeSuccess) throw new InvalidOperationException("simulated delivery failure");
        return Task.CompletedTask;
    }
}

sealed class CapturingTransport : IPaymentRequestEventTransport
{
    public PaymentRequestEventDispatch? LastDispatch { get; private set; }

    public Task DispatchAsync(PaymentRequestEventDispatch dispatch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastDispatch = dispatch;
        return Task.CompletedTask;
    }
}

sealed class FailingTransport(PaymentRequestEventTransportFailureKind failureKind) : IPaymentRequestEventTransport
{
    public Task DispatchAsync(PaymentRequestEventDispatch dispatch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new PaymentRequestEventTransportException(failureKind, $"simulated {failureKind} transport failure");
    }
}
