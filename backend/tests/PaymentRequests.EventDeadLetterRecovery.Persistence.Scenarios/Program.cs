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

var options = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new PaymentRequestDbContext(options);
await db.Database.EnsureCreatedAsync();

var store = new EfPaymentRequestEventDeadLetterRecoveryStore(db);
var now = new DateTimeOffset(2026, 9, 18, 18, 0, 0, TimeSpan.Zero);
var eventId = Guid.NewGuid();
var requestId = PaymentRequestId.From(Guid.NewGuid());

db.PaymentRequestEventOutbox.Add(new PaymentRequestEventOutboxEntity
{
    EventId = eventId,
    PaymentRequestId = requestId.Value,
    EventType = "payment_request.paid",
    PayloadJson = "{}",
    OccurredAtUtc = now.AddMinutes(-10).UtcDateTime,
    EnqueuedAtUtc = now.AddMinutes(-10).UtcDateTime,
    AvailableAtUtc = now.AddMinutes(-1).UtcDateTime,
    Status = (int)PaymentRequestEventOutboxStatus.DeadLetter,
    AttemptCount = 5,
    LastAttemptAtUtc = now.AddMinutes(-1).UtcDateTime,
    LastError = "Provider rejected delivery."
});

for (var attemptNumber = 1; attemptNumber <= 5; attemptNumber++)
{
    db.PaymentRequestEventAttempts.Add(new PaymentRequestEventAttemptEntity
    {
        AttemptId = Guid.NewGuid(),
        EventId = eventId,
        AttemptNumber = attemptNumber,
        StartedAtUtc = now.AddMinutes(-10 + attemptNumber).UtcDateTime,
        CompletedAtUtc = now.AddMinutes(-10 + attemptNumber).AddSeconds(1).UtcDateTime,
        Outcome = (int)(attemptNumber == 5
            ? PaymentRequestEventAttemptOutcome.DeadLetter
            : PaymentRequestEventAttemptOutcome.RetryScheduled),
        Error = "failure",
        NextAttemptAtUtc = attemptNumber == 5
            ? null
            : now.AddMinutes(-9 + attemptNumber).UtcDateTime
    });
}

await db.SaveChangesAsync();

var before = await db.PaymentRequestEventAttempts
    .AsNoTracking()
    .Where(x => x.EventId == eventId)
    .OrderBy(x => x.AttemptNumber)
    .Select(x => new
    {
        x.AttemptId,
        x.EventId,
        x.AttemptNumber,
        x.StartedAtUtc,
        x.CompletedAtUtc,
        x.Outcome,
        x.Error,
        x.NextAttemptAtUtc
    })
    .ToArrayAsync();

var loaded = await store.GetAsync(eventId);
Assert(loaded is not null, "Dead-letter event must be readable through the recovery store.");
Assert(loaded!.Status == PaymentRequestEventOutboxStatus.DeadLetter, "Recovery store must preserve DeadLetter status.");
Assert(loaded.AttemptCount == 5, "Recovery store must preserve attempt count.");
Assert(loaded.LastError == "Provider rejected delivery.", "Recovery store must preserve durable failure evidence.");

var plan = new PaymentRequestEventDeadLetterReplayPlan(
    eventId,
    ExpectedAttemptCount: 5,
    ReplayOrdinal: 1,
    AvailableAtUtc: now.AddMinutes(2),
    RequestedBy: "ops.user",
    Reason: "provider configuration corrected");

Assert(await store.TryRequeueAsync(plan), "Matching DeadLetter row must be atomically requeued.");

var requeued = await db.PaymentRequestEventOutbox
    .AsNoTracking()
    .SingleAsync(x => x.EventId == eventId);

Assert(requeued.Status == (int)PaymentRequestEventOutboxStatus.Retry, "DeadLetter must transition to Retry.");
Assert(requeued.AvailableAtUtc == plan.AvailableAtUtc.UtcDateTime, "Replay availability must be persisted.");
Assert(requeued.AttemptCount == 5, "Requeue must not increment attempt count.");
Assert(requeued.LastAttemptAtUtc == now.AddMinutes(-1).UtcDateTime, "Requeue must preserve last attempt evidence.");
Assert(requeued.LastError == "Provider rejected delivery.", "Requeue must preserve last error evidence.");
Assert(requeued.LeaseToken is null && requeued.LeaseExpiresAtUtc is null, "Requeue must not retain a processing lease.");
Assert(requeued.DeliveredAtUtc is null, "Requeue must not mark the event delivered.");

var after = await db.PaymentRequestEventAttempts
    .AsNoTracking()
    .Where(x => x.EventId == eventId)
    .OrderBy(x => x.AttemptNumber)
    .Select(x => new
    {
        x.AttemptId,
        x.EventId,
        x.AttemptNumber,
        x.StartedAtUtc,
        x.CompletedAtUtc,
        x.Outcome,
        x.Error,
        x.NextAttemptAtUtc
    })
    .ToArrayAsync();

Assert(before.Length == after.Length, "Atomic requeue must not add an Attempt Ledger row.");
for (var i = 0; i < before.Length; i++)
{
    Assert(before[i].Equals(after[i]), "Atomic requeue must not mutate Attempt Ledger history.");
}

Assert(!await store.TryRequeueAsync(plan), "Already requeued event must fail closed on repeated replay.");

var staleEventId = Guid.NewGuid();
db.PaymentRequestEventOutbox.Add(new PaymentRequestEventOutboxEntity
{
    EventId = staleEventId,
    PaymentRequestId = Guid.NewGuid(),
    EventType = "payment_request.cancelled",
    PayloadJson = "{}",
    OccurredAtUtc = now.UtcDateTime,
    EnqueuedAtUtc = now.UtcDateTime,
    AvailableAtUtc = now.UtcDateTime,
    Status = (int)PaymentRequestEventOutboxStatus.DeadLetter,
    AttemptCount = 4,
    LastAttemptAtUtc = now.UtcDateTime,
    LastError = "permanent"
});
await db.SaveChangesAsync();

var stalePlan = new PaymentRequestEventDeadLetterReplayPlan(
    staleEventId,
    ExpectedAttemptCount: 3,
    ReplayOrdinal: 1,
    AvailableAtUtc: now.AddMinutes(1),
    RequestedBy: "ops",
    Reason: "retry");

Assert(!await store.TryRequeueAsync(stalePlan), "Stale ExpectedAttemptCount must fail optimistic requeue.");

var staleRow = await db.PaymentRequestEventOutbox.AsNoTracking()
    .SingleAsync(x => x.EventId == staleEventId);
Assert(staleRow.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter, "Optimistic mismatch must not mutate status.");
Assert(staleRow.AttemptCount == 4, "Optimistic mismatch must not mutate attempt count.");

var pendingEventId = Guid.NewGuid();
db.PaymentRequestEventOutbox.Add(new PaymentRequestEventOutboxEntity
{
    EventId = pendingEventId,
    PaymentRequestId = Guid.NewGuid(),
    EventType = "payment_request.created",
    PayloadJson = "{}",
    OccurredAtUtc = now.UtcDateTime,
    EnqueuedAtUtc = now.UtcDateTime,
    AvailableAtUtc = now.UtcDateTime,
    Status = (int)PaymentRequestEventOutboxStatus.Pending,
    AttemptCount = 1
});
await db.SaveChangesAsync();

Assert(
    !await store.TryRequeueAsync(new(
        pendingEventId,
        ExpectedAttemptCount: 1,
        ReplayOrdinal: 1,
        AvailableAtUtc: now,
        RequestedBy: "ops",
        Reason: "retry")),
    "Only DeadLetter rows may be requeued.");

Assert(await store.GetAsync(Guid.NewGuid()) is null, "Unknown event must return null.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => store.GetAsync(eventId, cts.Token),
    "GetAsync must propagate cancellation.");

await AssertThrowsAsync<OperationCanceledException>(
    () => store.TryRequeueAsync(
        new(
            staleEventId,
            ExpectedAttemptCount: 4,
            ReplayOrdinal: 1,
            AvailableAtUtc: now,
            RequestedBy: "ops",
            Reason: "retry"),
        cts.Token),
    "TryRequeueAsync must propagate cancellation.");

Console.WriteLine(
    "AFW-BE-REQUEST-OUTBOX-RECOVERY-1 persistence adapter and atomic requeue scenarios: PASS");
