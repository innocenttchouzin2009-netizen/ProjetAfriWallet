using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(
    Path.GetTempPath(),
    $"afw-payment-request-recovery-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";
var leaseDuration = TimeSpan.FromMinutes(5);
var startedAt = new DateTimeOffset(2026, 9, 20, 17, 0, 0, TimeSpan.Zero);
var recoveredAt = startedAt.Add(leaseDuration);

try
{
    var openAttemptEventId = Guid.NewGuid();
    var missingAttemptEventId = Guid.NewGuid();

    await using (var beforeCrash = CreateDb(connectionString))
    {
        await beforeCrash.Database.EnsureCreatedAsync();
        var store = new EfPaymentRequestEventOutboxStore(beforeCrash);
        var ledger = new EfPaymentRequestEventAttemptLedger(beforeCrash);

        Assert(
            await store.EnqueueAsync(
                Envelope(openAttemptEventId, startedAt),
                startedAt),
            "Open-attempt event must be enqueued.");

        var firstClaim = await store.ClaimBatchAsync(1, startedAt, leaseDuration);
        Assert(
            firstClaim.Count == 1 &&
            firstClaim[0].Event.EventId == openAttemptEventId &&
            firstClaim[0].AttemptCount == 1,
            "Open-attempt event must be claimed as attempt 1.");

        await ledger.BeginAttemptAsync(
            openAttemptEventId,
            firstClaim[0].AttemptCount,
            startedAt);

        Assert(
            await store.EnqueueAsync(
                Envelope(missingAttemptEventId, startedAt.AddSeconds(1)),
                startedAt.AddSeconds(1)),
            "Missing-attempt event must be enqueued.");

        var secondClaim = await store.ClaimBatchAsync(
            1,
            startedAt.AddSeconds(1),
            leaseDuration);
        Assert(
            secondClaim.Count == 1 &&
            secondClaim[0].Event.EventId == missingAttemptEventId &&
            secondClaim[0].AttemptCount == 1,
            "Missing-attempt event must be claimed as attempt 1.");

        // Simulate process termination here:
        // first event has an open Attempt Ledger row;
        // second event was claimed before BeginAttemptAsync persisted anything.
    }

    await using (var afterRestart = CreateDb(connectionString))
    {
        var recovery = new EfPaymentRequestEventRecoveryStore(afterRestart);
        var recovered = await recovery.RecoverExpiredClaimsAsync(
            10,
            recoveredAt.AddSeconds(1));

        Assert(recovered == 2, "Restart recovery must recover both expired Processing claims.");

        foreach (var eventId in new[] { openAttemptEventId, missingAttemptEventId })
        {
            var outbox = await afterRestart.PaymentRequestEventOutbox.AsNoTracking()
                .SingleAsync(x => x.EventId == eventId);
            Assert(
                outbox.Status == (int)PaymentRequestEventOutboxStatus.Retry,
                "Recovered Outbox row must return to Retry.");
            Assert(
                outbox.LeaseToken is null && outbox.LeaseExpiresAtUtc is null,
                "Recovered Outbox row must release its lease.");
            Assert(
                outbox.AvailableAtUtc == recoveredAt.AddSeconds(1).UtcDateTime,
                "Recovered Outbox row must be immediately retryable.");

            var attempt = await afterRestart.PaymentRequestEventAttempts.AsNoTracking()
                .SingleAsync(x => x.EventId == eventId && x.AttemptNumber == 1);
            Assert(attempt.CompletedAtUtc is not null, "Recovered attempt must be closed.");
            Assert(
                attempt.Outcome == (int)PaymentRequestEventAttemptOutcome.RetryScheduled,
                "Recovered attempt must record RetryScheduled.");
            Assert(
                attempt.NextAttemptAtUtc == recoveredAt.AddSeconds(1).UtcDateTime,
                "Recovered attempt must record the same retry eligibility time.");
        }

        var secondRecovery = await recovery.RecoverExpiredClaimsAsync(
            10,
            recoveredAt.AddSeconds(2));
        Assert(secondRecovery == 0, "Recovery must be idempotent once leases are cleared.");
        Assert(
            await afterRestart.PaymentRequestEventAttempts.CountAsync() == 2,
            "Idempotent recovery must not duplicate Attempt Ledger rows.");
    }

    await using (var resumedProcess = CreateDb(connectionString))
    {
        var store = new EfPaymentRequestEventOutboxStore(resumedProcess);
        var ledger = new EfPaymentRequestEventAttemptLedger(resumedProcess);
        var finalizer = new EfPaymentRequestEventAttemptFinalizer(resumedProcess);
        var delivery = new RecordingDeliveryPort();
        var processor = new PaymentRequestEventOutboxProcessor(
            store,
            delivery,
            ledger,
            finalizer,
            new PaymentRequestEventDeliveryOptions(
                5,
                leaseDuration,
                TimeSpan.FromSeconds(30)));

        var delivered = await processor.ProcessBatchAsync(
            10,
            recoveredAt.AddSeconds(2));

        Assert(delivered == 2, "Recovered events must be deliverable after restart.");
        Assert(
            delivery.EventIds.Count == 2,
            "Each recovered event must be redelivered exactly once in this cycle.");

        foreach (var eventId in new[] { openAttemptEventId, missingAttemptEventId })
        {
            var outbox = await resumedProcess.PaymentRequestEventOutbox.AsNoTracking()
                .SingleAsync(x => x.EventId == eventId);
            Assert(
                outbox.Status == (int)PaymentRequestEventOutboxStatus.Delivered,
                "Recovered event must reach Delivered after successful retry.");
            Assert(
                outbox.AttemptCount == 2,
                "Recovered event must continue with the next monotonic attempt number.");

            var attempts = await resumedProcess.PaymentRequestEventAttempts.AsNoTracking()
                .Where(x => x.EventId == eventId)
                .OrderBy(x => x.AttemptNumber)
                .ToArrayAsync();
            Assert(
                attempts.Length == 2 &&
                attempts[0].AttemptNumber == 1 &&
                attempts[1].AttemptNumber == 2,
                "Restart recovery must preserve contiguous attempt numbering.");
            Assert(
                attempts.All(x => x.CompletedAtUtc is not null),
                "No open Attempt Ledger row may survive successful restart recovery.");
            Assert(
                attempts[1].Outcome == (int)PaymentRequestEventAttemptOutcome.Delivered,
                "Post-restart retry attempt must be recorded as Delivered.");
        }
    }

    Console.WriteLine(
        "AFW-BE-REQUEST-OUTBOX-RESTART-RECOVERY-1 durable restart-safe recovery scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath))
        File.Delete(dbPath);
}

static PaymentRequestDbContext CreateDb(string connectionString) =>
    new(
        new DbContextOptionsBuilder<PaymentRequestDbContext>()
            .UseSqlite(connectionString)
            .Options);

static PaymentRequestEventEnvelope Envelope(
    Guid eventId,
    DateTimeOffset occurredAtUtc) =>
    new(
        eventId,
        PaymentRequestId.From(Guid.NewGuid()),
        "payment-request.paid",
        occurredAtUtc,
        """{"status":"Paid"}""");

sealed class RecordingDeliveryPort : IPaymentRequestEventDeliveryPort
{
    public List<Guid> EventIds { get; } = [];

    public Task DeliverAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EventIds.Add(paymentRequestEvent.EventId);
        return Task.CompletedTask;
    }
}
