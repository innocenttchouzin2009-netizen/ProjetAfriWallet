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
        PayloadJson = "{}",
        OccurredAtUtc = now.AddMinutes(i),
        EnqueuedAtUtc = now.AddMinutes(i),
        AvailableAtUtc = now.AddMinutes(i),
        Status = (int)statuses[i],
        AttemptCount = statuses[i] == PaymentRequestEventOutboxStatus.Pending ? 0 : 1,
        LastAttemptAtUtc = statuses[i] is PaymentRequestEventOutboxStatus.DeadLetter or PaymentRequestEventOutboxStatus.Delivered
            ? now.AddMinutes(i + 1)
            : null,
        DeliveredAtUtc = statuses[i] == PaymentRequestEventOutboxStatus.Delivered ? now.AddMinutes(i + 1) : null,
        LastError = statuses[i] == PaymentRequestEventOutboxStatus.DeadLetter ? "terminal delivery failure" : null
    });
}
await db.SaveChangesAsync();

var diagnostics = new EfPaymentRequestEventOutboxDiagnostics(db);
var persisted = await diagnostics.GetSnapshotAsync();
Assert(persisted.PendingCount == 1, "Pending count mismatch.");
Assert(persisted.ProcessingCount == 1, "Processing count mismatch.");
Assert(persisted.RetryCount == 1, "Retry count mismatch.");
Assert(persisted.DeliveredCount == 1, "Delivered count mismatch.");
Assert(persisted.DeadLetterCount == 1, "Dead-letter count mismatch.");
Assert(persisted.OldestUndeliveredAtUtc == new DateTimeOffset(now), "Oldest undelivered timestamp mismatch.");
Assert(persisted.LatestDeadLetterAtUtc == new DateTimeOffset(now.AddMinutes(5)), "Latest dead-letter timestamp mismatch.");

var state = new PaymentRequestEventOutboxDispatcherState();
var started = new DateTimeOffset(2026, 9, 15, 16, 10, 0, TimeSpan.Zero);
state.MarkCycleStarted(started);
state.MarkCycleCompleted(started.AddSeconds(1), 3);
state.MarkCycleStarted(started.AddSeconds(2));
state.MarkCycleFailed(started.AddSeconds(3), "boom");
var stateSnapshot = state.Snapshot;
Assert(stateSnapshot.TotalCycleCount == 2, "Total cycle count mismatch.");
Assert(stateSnapshot.TotalFailedCycleCount == 1, "Failed cycle count mismatch.");
Assert(stateSnapshot.TotalDeliveredCount == 3, "Total delivered count mismatch.");
Assert(stateSnapshot.ConsecutiveFailures == 1, "Consecutive failure count mismatch.");
Assert(stateSnapshot.Status == PaymentRequestEventOutboxDispatcherStatus.Faulted, "Expected Faulted state.");

var services = new ServiceCollection();
services.AddSingleton<IPaymentRequestEventDeliveryPort, NoopDeliveryPort>();
using var provider = services.BuildServiceProvider();
var enabledOptions = PaymentRequestEventOutboxHostingOptions.Default with { Enabled = true };
var health = new PaymentRequestEventOutboxOperationalHealthService(state, enabledOptions, diagnostics, provider);
var faulted = await health.GetSnapshotAsync();
Assert(!faulted.Ready && faulted.Status == PaymentRequestEventOutboxDispatcherStatus.Faulted, "Faulted dispatcher must not be ready.");
Assert(faulted.DeadLetterCount == 1, "Operational snapshot must expose dead-letter count.");

state.MarkIdle();
var ready = await health.GetSnapshotAsync();
Assert(ready.Ready, "Idle enabled dispatcher with delivery port must be ready.");

var disabledHealth = new PaymentRequestEventOutboxOperationalHealthService(
    state,
    PaymentRequestEventOutboxHostingOptions.Default,
    diagnostics,
    provider);
var disabled = await disabledHealth.GetSnapshotAsync();
Assert(!disabled.Ready, "Disabled dispatcher must not be ready.");

Console.WriteLine("AFW-BE-REQUEST-EVENT-OUTBOX-HOSTING-1 operational health scenarios: PASS");

sealed class NoopDeliveryPort : IPaymentRequestEventDeliveryPort
{
    public Task DeliverAsync(PaymentRequestEventEnvelope paymentRequestEvent, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
