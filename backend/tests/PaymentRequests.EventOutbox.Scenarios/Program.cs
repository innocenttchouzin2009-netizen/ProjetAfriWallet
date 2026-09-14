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
var secondPass = await processor.ProcessBatchAsync(10, now.AddMinutes(1).AddSeconds(10));
Assert(secondPass == 1, "Second delivery attempt must succeed.");

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

Console.WriteLine("AFW-BE-REQUEST-EVENTS-1 durable event outbox scenarios: PASS");

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
