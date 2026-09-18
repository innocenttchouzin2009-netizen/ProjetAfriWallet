using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>().UseSqlite(connection).Options;
await using var db = new PaymentRequestDbContext(options);
await db.Database.EnsureCreatedAsync();

var store = new EfPaymentRequestEventOutboxStore(db);
var ledger = new EfPaymentRequestEventAttemptLedger(db);
var transport = new FailOnceDeliveryPort();
var processor = new PaymentRequestEventOutboxProcessor(
    store, transport, ledger,
    new PaymentRequestEventDeliveryOptions(3, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30)));

var now = new DateTimeOffset(2026, 9, 18, 16, 0, 0, TimeSpan.Zero);
var eventId = Guid.NewGuid();
var envelope = new PaymentRequestEventEnvelope(eventId, PaymentRequestId.From(Guid.NewGuid()), "PaymentRequestCreated", now, "{}");
Assert(await store.EnqueueAsync(envelope, now), "Event must be enqueued.");

var firstDelivered = await processor.ProcessBatchAsync(10, now);
Assert(firstDelivered == 0, "First transient failure must not count as delivered.");
var firstAttempts = await ledger.ListByEventAsync(eventId);
Assert(firstAttempts.Count == 1, "First provider attempt must create exactly one ledger row.");
Assert(firstAttempts[0].AttemptNumber == 1, "First attempt number mismatch.");
Assert(firstAttempts[0].Outcome == PaymentRequestEventAttemptOutcome.RetryScheduled, "First attempt must record retry scheduling.");
Assert(firstAttempts[0].NextAttemptAtUtc == now.AddSeconds(30), "Attempt ledger must record the Outbox-computed retry time.");

var tooEarly = await processor.ProcessBatchAsync(10, now.AddSeconds(29));
Assert(tooEarly == 0, "Outbox must remain the sole source of retry eligibility.");
Assert((await ledger.ListByEventAsync(eventId)).Count == 1, "No eligible Outbox item means no new attempt row.");

var secondDelivered = await processor.ProcessBatchAsync(10, now.AddSeconds(30));
Assert(secondDelivered == 1, "Second attempt must deliver.");
var attempts = await ledger.ListByEventAsync(eventId);
Assert(attempts.Count == 2, "Two provider attempts must create exactly two ledger rows.");
Assert(attempts.Select(x => x.AttemptNumber).SequenceEqual(new[] { 1, 2 }), "Attempt numbers must be monotonic and unique.");
Assert(attempts[1].Outcome == PaymentRequestEventAttemptOutcome.Delivered, "Second attempt must be delivered.");

var outbox = await db.PaymentRequestEventOutbox.AsNoTracking().SingleAsync(x => x.EventId == eventId);
Assert(outbox.Status == (int)PaymentRequestEventOutboxStatus.Delivered, "Outbox must own terminal delivery state.");
Assert(outbox.AttemptCount == 2, "Outbox attempt count must match Attempt Ledger cardinality.");

var duplicateAttemptId1 = await ledger.BeginAttemptAsync(eventId, 2, now.AddSeconds(30));
var duplicateAttemptId2 = await ledger.BeginAttemptAsync(eventId, 2, now.AddSeconds(30));
Assert(duplicateAttemptId1 == duplicateAttemptId2, "Duplicate begin for same event/attempt must be idempotent.");
Assert((await ledger.ListByEventAsync(eventId)).Count == 2, "Duplicate begin must not create a second ledger row.");

Console.WriteLine("AFW-BE-REQUEST-OUTBOX-ATTEMPT-1 attempt ledger invariant scenarios: PASS");

sealed class FailOnceDeliveryPort : IPaymentRequestEventDeliveryPort
{
    private int calls;
    public Task DeliverAsync(PaymentRequestEventEnvelope paymentRequestEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        calls++;
        if (calls == 1) throw new PaymentRequestEventDeliveryException(PaymentRequestEventDeliveryFailureKind.Transient, "temporary");
        return Task.CompletedTask;
    }
}
