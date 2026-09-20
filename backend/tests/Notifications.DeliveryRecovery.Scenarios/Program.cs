using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 20, 20, 0, 0, TimeSpan.Zero);
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<NotificationDeliveryDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new NotificationDeliveryDbContext(options);
await db.Database.EnsureCreatedAsync();

var repository = new EfNotificationDeliveryRepository(db);

// 1. Normal recovery: a persisted Pending delivery is reclaimed and dispatched once.
var userId = Guid.NewGuid();
var createdEvent = PaymentRequestEvent.New(
    Guid.NewGuid(),
    PaymentRequestEventKind.Created,
    now);
var notification = InAppNotification.New(userId, createdEvent);
var pending = NotificationDelivery.From(notification, NotificationChannel.InApp);
await repository.GetOrAddAsync(pending);

var inAppDispatcher = new RecordingDispatcher(NotificationChannel.InApp);
var recovery = new NotificationDeliveryRecoveryService(
    repository,
    [inAppDispatcher],
    new FixedTimeProvider(now.AddMinutes(1)),
    NotificationDeliveryRecoveryOptions.Default);

var first = await recovery.RecoverAsync();
Assert(first == new NotificationDeliveryRecoveryResult(1, 1, 0),
    "Pending delivery must be claimed and dispatched.");
Assert(inAppDispatcher.Calls == 1, "Recovered delivery must dispatch exactly once.");
Assert(await repository.GetAsync(notification.EventId, NotificationChannel.InApp, userId)
    is { Status: NotificationDeliveryStatus.Dispatched },
    "Recovered delivery must be marked dispatched.");

var replay = await recovery.RecoverAsync();
Assert(replay.Claimed == 0 && inAppDispatcher.Calls == 1,
    "Already dispatched delivery must not be recovered again.");

// 2. Failed dispatch releases its claim and becomes immediately retryable.
var failedUserId = Guid.NewGuid();
var acceptedEvent = PaymentRequestEvent.New(
    Guid.NewGuid(),
    PaymentRequestEventKind.Accepted,
    now.AddMinutes(2));
var failedNotification = InAppNotification.New(failedUserId, acceptedEvent);
await repository.GetOrAddAsync(NotificationDelivery.From(failedNotification, NotificationChannel.Push));

var failOnceDispatcher = new FailOnceDispatcher(NotificationChannel.Push);
var retryRecovery = new NotificationDeliveryRecoveryService(
    repository,
    [failOnceDispatcher],
    new FixedTimeProvider(now.AddMinutes(3)),
    NotificationDeliveryRecoveryOptions.Default);

var failedAttempt = await retryRecovery.RecoverAsync();
Assert(failedAttempt == new NotificationDeliveryRecoveryResult(1, 0, 1),
    "Transient recovery failure must be reported without marking delivery dispatched.");
Assert(await repository.GetAsync(failedNotification.EventId, NotificationChannel.Push, failedUserId)
    is { Status: NotificationDeliveryStatus.Pending },
    "Failed recovery must leave delivery pending.");

var successfulRetry = await retryRecovery.RecoverAsync();
Assert(successfulRetry == new NotificationDeliveryRecoveryResult(1, 1, 0),
    "Released recovery claim must be retryable.");
Assert(failOnceDispatcher.Calls == 2, "Retry must perform one second dispatch attempt.");

// 3. Active lease prevents a second worker claim; expired lease enables crash recovery.
var crashedUserId = Guid.NewGuid();
var declinedEvent = PaymentRequestEvent.New(
    Guid.NewGuid(),
    PaymentRequestEventKind.Declined,
    now.AddMinutes(4));
var crashedNotification = InAppNotification.New(crashedUserId, declinedEvent);
await repository.GetOrAddAsync(NotificationDelivery.From(crashedNotification, NotificationChannel.InApp));

var claimTime = now.AddMinutes(5);
var workerOne = await repository.ClaimRecoverableAsync(
    claimTime,
    claimTime.AddMinutes(2),
    10);
Assert(workerOne.Count == 1, "First worker must claim pending delivery.");

var workerTwo = await repository.ClaimRecoverableAsync(
    claimTime.AddSeconds(30),
    claimTime.AddMinutes(2).AddSeconds(30),
    10);
Assert(workerTwo.Count == 0, "Active recovery lease must exclude a second worker.");

var afterCrash = await repository.ClaimRecoverableAsync(
    claimTime.AddMinutes(3),
    claimTime.AddMinutes(5),
    10);
Assert(afterCrash.Count == 1 && afterCrash[0].DeliveryId == workerOne[0].DeliveryId,
    "Expired lease must make an abandoned delivery recoverable after worker crash.");

await repository.ReleaseRecoveryClaimAsync(afterCrash[0].DeliveryId);

Console.WriteLine("AFW-BE-NOTIFICATION-DELIVERY-RECOVERY-1 scenarios: PASS");

sealed class RecordingDispatcher(NotificationChannel channel) : INotificationChannelDispatchPort
{
    public NotificationChannel Channel { get; } = channel;
    public int Calls { get; private set; }

    public Task DispatchAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (delivery.Channel != Channel)
            throw new InvalidOperationException("Dispatcher channel mismatch.");
        Calls++;
        return Task.CompletedTask;
    }
}

sealed class FailOnceDispatcher(NotificationChannel channel) : INotificationChannelDispatchPort
{
    public NotificationChannel Channel { get; } = channel;
    public int Calls { get; private set; }

    public Task DispatchAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        if (Calls == 1)
            throw new InvalidOperationException("Simulated transient dispatch failure.");
        return Task.CompletedTask;
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
