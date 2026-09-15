using System.Text.Json;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using IdentityService.Api.PaymentRequests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var dbOptions = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(connection)
    .Options;
await using var db = new PaymentRequestDbContext(dbOptions);
await db.Database.EnsureCreatedAsync();

var now = new DateTime(2026, 9, 15, 16, 0, 0, DateTimeKind.Utc);
var statuses = new[]
{
    PaymentRequestEventOutboxStatus.Pending,
    PaymentRequestEventOutboxStatus.Processing,
    PaymentRequestEventOutboxStatus.Retry,
    PaymentRequestEventOutboxStatus.Delivered,
    PaymentRequestEventOutboxStatus.DeadLetter
};

for (var i = 0; i < statuses.Length; i++)
{
    db.PaymentRequestEventOutbox.Add(new PaymentRequestEventOutboxEntity
    {
        EventId = Guid.NewGuid(),
        PaymentRequestId = Guid.NewGuid(),
        EventType = "PaymentRequest.Test",
        PayloadJson = "{\"secret\":\"do-not-expose\"}",
        OccurredAtUtc = now.AddMinutes(i),
        EnqueuedAtUtc = now.AddMinutes(i),
        AvailableAtUtc = now.AddMinutes(i),
        Status = (int)statuses[i],
        AttemptCount = statuses[i] == PaymentRequestEventOutboxStatus.Pending ? 0 : 1,
        LastAttemptAtUtc = statuses[i] is PaymentRequestEventOutboxStatus.DeadLetter or PaymentRequestEventOutboxStatus.Delivered
            ? now.AddMinutes(i + 1)
            : null,
        DeliveredAtUtc = statuses[i] == PaymentRequestEventOutboxStatus.Delivered ? now.AddMinutes(i + 1) : null,
        LastError = statuses[i] == PaymentRequestEventOutboxStatus.DeadLetter ? "secret failure details" : null
    });
}
await db.SaveChangesAsync();

var diagnostics = new EfPaymentRequestEventOutboxDiagnostics(db);
var backlog = await diagnostics.GetSnapshotAsync();
Assert(backlog.PendingCount == 1, "Pending count mismatch.");
Assert(backlog.ProcessingCount == 1, "Processing count mismatch.");
Assert(backlog.RetryCount == 1, "Retry count mismatch.");
Assert(backlog.DeliveredCount == 1, "Delivered count mismatch.");
Assert(backlog.DeadLetterCount == 1, "Dead-letter count mismatch.");
Assert(backlog.OldestUndeliveredAtUtc == new DateTimeOffset(now), "Oldest undelivered timestamp mismatch.");
Assert(backlog.LatestDeadLetterAtUtc == new DateTimeOffset(now.AddMinutes(5)), "Latest dead-letter timestamp mismatch.");

var state = new PaymentRequestEventOutboxWorkerState();
var started = new DateTimeOffset(2026, 9, 15, 16, 10, 0, TimeSpan.Zero);
state.MarkCycleStarted(started);
state.MarkCycleSucceeded(started.AddSeconds(1), 3);
var successfulAt = state.Snapshot.LastSuccessfulCycleCompletedAtUtc;
state.MarkCycleStarted(started.AddSeconds(2));
state.MarkCycleFailed(started.AddSeconds(3), "SecretException");
var stateSnapshot = state.Snapshot;
Assert(stateSnapshot.TotalCycleCount == 2, "Total cycle count mismatch.");
Assert(stateSnapshot.TotalFailedCycleCount == 1, "Failed cycle count mismatch.");
Assert(stateSnapshot.TotalDeliveredCount == 3, "Total delivered count mismatch.");
Assert(stateSnapshot.ConsecutiveFailures == 1, "Consecutive failure count mismatch.");
Assert(stateSnapshot.LastSuccessfulCycleCompletedAtUtc == successfulAt, "Failed cycle must not overwrite last successful cycle.");
Assert(stateSnapshot.Status == PaymentRequestEventOutboxWorkerStatus.Faulted, "Expected Faulted state.");

var services = new ServiceCollection();
services.AddSingleton<IPaymentRequestEventTransport, NoopTransport>();
using var provider = services.BuildServiceProvider();
var health = new PaymentRequestEventOutboxOperationalHealthService(state, diagnostics, provider);
var faulted = await health.GetSnapshotAsync();
Assert(!faulted.Ready, "Faulted worker must not be ready.");
Assert(faulted.DeadLetterCount == 1, "Operational snapshot must expose dead-letter count.");
Assert(faulted.LastSuccessfulCycleCompletedAtUtc == successfulAt, "Operational snapshot must expose the last successful cycle.");

var json = JsonSerializer.Serialize(faulted);
Assert(!json.Contains("do-not-expose", StringComparison.Ordinal), "Operational snapshot must not expose event payload content.");
Assert(!json.Contains("PayloadJson", StringComparison.Ordinal), "Operational snapshot must not expose payload fields.");
Assert(!json.Contains("secret failure details", StringComparison.Ordinal), "Operational snapshot must not expose persisted error details.");
Assert(!json.Contains("LastError", StringComparison.Ordinal), "Operational snapshot must not expose worker error text.");

var readyState = new PaymentRequestEventOutboxWorkerState();
readyState.MarkCycleStarted(started);
readyState.MarkCycleSucceeded(started.AddSeconds(1), 0);
var readyHealth = new PaymentRequestEventOutboxOperationalHealthService(readyState, diagnostics, provider);
Assert((await readyHealth.GetSnapshotAsync()).Ready, "Healthy idle worker with a transport must be ready.");

using var noTransportProvider = new ServiceCollection().BuildServiceProvider();
var noTransportHealth = new PaymentRequestEventOutboxOperationalHealthService(readyState, diagnostics, noTransportProvider);
Assert(!(await noTransportHealth.GetSnapshotAsync()).Ready, "Worker without a configured transport must not be ready.");

Console.WriteLine("AFW-BE-REQUEST-EVENTS-1 operational outbox observability scenarios: PASS");

sealed class NoopTransport : IPaymentRequestEventTransport
{
    public Task DispatchAsync(PaymentRequestEventDispatch dispatch, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
