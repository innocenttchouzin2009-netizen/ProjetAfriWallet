using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>().UseSqlite(connection).Options;
await using var db = new PaymentRequestDbContext(options);
await db.Database.EnsureCreatedAsync();

var store = new EfPaymentRequestEventOutboxStore(db);
var ledger = new EfPaymentRequestEventAttemptLedger(db);
var finalizer = new EfPaymentRequestEventAttemptFinalizer(db);
var transport = new FailOnceDeliveryPort();
var deliveryOptions = new PaymentRequestEventDeliveryOptions(
    3,
    TimeSpan.FromMinutes(5),
    TimeSpan.FromSeconds(30));

var processor = new PaymentRequestEventOutboxProcessor(
    store,
    transport,
    ledger,
    finalizer,
    deliveryOptions);

var now = new DateTimeOffset(2026, 9, 18, 16, 0, 0, TimeSpan.Zero);
var eventId = Guid.NewGuid();
var envelope = new PaymentRequestEventEnvelope(
    eventId,
    PaymentRequestId.From(Guid.NewGuid()),
    "PaymentRequestCreated",
    now,
    "{}");
Assert(await store.EnqueueAsync(envelope, now), "Event must be enqueued.");

var firstDelivered = await processor.ProcessBatchAsync(10, now);
Assert(firstDelivered == 0, "First transient failure must not count as delivered.");

var firstAttempts = await ledger.ListByEventAsync(eventId);
Assert(firstAttempts.Count == 1, "First provider attempt must create exactly one ledger row.");
Assert(firstAttempts[0].AttemptNumber == 1, "First attempt number mismatch.");
Assert(
    firstAttempts[0].Outcome == PaymentRequestEventAttemptOutcome.RetryScheduled,
    "First attempt must record retry scheduling.");
Assert(
    firstAttempts[0].NextAttemptAtUtc == now.AddSeconds(30),
    "Attempt ledger must record the Outbox-computed retry time.");

var retryOutbox = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == eventId);
Assert(
    retryOutbox.Status == (int)PaymentRequestEventOutboxStatus.Retry,
    "Attempt completion and Retry state must be persisted together.");
Assert(
    retryOutbox.AvailableAtUtc == now.AddSeconds(30).UtcDateTime,
    "Retry state must carry the same next-attempt timestamp as the Attempt Ledger.");

var tooEarly = await processor.ProcessBatchAsync(10, now.AddSeconds(29));
Assert(tooEarly == 0, "Outbox must remain the sole source of retry eligibility.");
Assert(
    (await ledger.ListByEventAsync(eventId)).Count == 1,
    "No eligible Outbox item means no new attempt row.");

var secondDelivered = await processor.ProcessBatchAsync(10, now.AddSeconds(30));
Assert(secondDelivered == 1, "Second attempt must deliver.");

var attempts = await ledger.ListByEventAsync(eventId);
Assert(attempts.Count == 2, "Two provider attempts must create exactly two ledger rows.");
Assert(
    attempts.Select(x => x.AttemptNumber).SequenceEqual(new[] { 1, 2 }),
    "Attempt numbers must be monotonic and unique.");
Assert(
    attempts[1].Outcome == PaymentRequestEventAttemptOutcome.Delivered,
    "Second attempt must be delivered.");

var deliveredOutbox = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == eventId);
Assert(
    deliveredOutbox.Status == (int)PaymentRequestEventOutboxStatus.Delivered,
    "Attempt completion and Delivered state must be persisted together.");
Assert(
    deliveredOutbox.AttemptCount == 2,
    "Outbox attempt count must match Attempt Ledger cardinality.");

var duplicateAttemptId1 = await ledger.BeginAttemptAsync(eventId, 2, now.AddSeconds(30));
var duplicateAttemptId2 = await ledger.BeginAttemptAsync(eventId, 2, now.AddSeconds(30));
Assert(
    duplicateAttemptId1 == duplicateAttemptId2,
    "Duplicate begin for same event/attempt must be idempotent.");
Assert(
    (await ledger.ListByEventAsync(eventId)).Count == 2,
    "Duplicate begin must not create a second ledger row.");

var deadLetterAt = now.AddMinutes(1);
var deadLetterEventId = Guid.NewGuid();
Assert(
    await store.EnqueueAsync(
        new PaymentRequestEventEnvelope(
            deadLetterEventId,
            PaymentRequestId.From(Guid.NewGuid()),
            "PaymentRequestCancelled",
            deadLetterAt,
            "{}"),
        deadLetterAt),
    "Dead-letter event must be enqueued.");

var deadLetterProcessor = new PaymentRequestEventOutboxProcessor(
    store,
    new PermanentFailureDeliveryPort(),
    ledger,
    finalizer,
    deliveryOptions);

var deadLetterDelivered = await deadLetterProcessor.ProcessBatchAsync(10, deadLetterAt);
Assert(deadLetterDelivered == 0, "Permanent failure must not count as delivered.");

var deadLetterAttempt = (await ledger.ListByEventAsync(deadLetterEventId)).Single();
Assert(
    deadLetterAttempt.Outcome == PaymentRequestEventAttemptOutcome.DeadLetter,
    "Permanent failure must close Attempt Ledger as DeadLetter.");

var deadLetterOutbox = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == deadLetterEventId);
Assert(
    deadLetterOutbox.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter,
    "Attempt completion and DeadLetter state must be persisted together.");

var rollbackAt = now.AddMinutes(2);
var rollbackEventId = Guid.NewGuid();
Assert(
    await store.EnqueueAsync(
        new PaymentRequestEventEnvelope(
            rollbackEventId,
            PaymentRequestId.From(Guid.NewGuid()),
            "PaymentRequestAccepted",
            rollbackAt,
            "{}"),
        rollbackAt),
    "Rollback event must be enqueued.");

var claimed = await store.ClaimBatchAsync(
    1,
    rollbackAt,
    TimeSpan.FromMinutes(5));
Assert(claimed.Count == 1 && claimed[0].Event.EventId == rollbackEventId, "Rollback event must be claimed.");

var rollbackAttemptId = await ledger.BeginAttemptAsync(
    rollbackEventId,
    claimed[0].AttemptCount,
    rollbackAt);

await db.PaymentRequestEventOutbox
    .Where(x => x.EventId == rollbackEventId)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Pending));

await AssertThrowsAsync<InvalidOperationException>(
    () => finalizer.FinalizeDeliveredAsync(
        rollbackAttemptId,
        rollbackEventId,
        rollbackAt),
    "Finalization must fail when the Outbox row is no longer Processing.");

var rolledBackAttempt = (await ledger.ListByEventAsync(rollbackEventId)).Single();
Assert(
    rolledBackAttempt.CompletedAtUtc is null &&
    rolledBackAttempt.Outcome is null,
    "Failed Outbox transition must roll back Attempt Ledger completion.");

var rolledBackOutbox = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == rollbackEventId);
Assert(
    rolledBackOutbox.Status == (int)PaymentRequestEventOutboxStatus.Pending,
    "Failed atomic finalization must not mutate the conflicting Outbox state.");

Console.WriteLine(
    "AFW-BE-REQUEST-OUTBOX-ATTEMPT-1 atomic attempt finalization scenarios: PASS");

sealed class FailOnceDeliveryPort : IPaymentRequestEventDeliveryPort
{
    private int calls;

    public Task DeliverAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        calls++;

        if (calls == 1)
        {
            throw new PaymentRequestEventDeliveryException(
                PaymentRequestEventDeliveryFailureKind.Transient,
                "temporary");
        }

        return Task.CompletedTask;
    }
}

sealed class PermanentFailureDeliveryPort : IPaymentRequestEventDeliveryPort
{
    public Task DeliverAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new PaymentRequestEventDeliveryException(
            PaymentRequestEventDeliveryFailureKind.Permanent,
            "permanent");
    }
}
