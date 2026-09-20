using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 20, 19, 30, 0, TimeSpan.Zero);
var time = new FixedTimeProvider(now.AddMinutes(1));
var userId = Guid.NewGuid();

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<NotificationDeliveryDbContext>()
    .UseSqlite(connection)
    .Options;
await using var db = new NotificationDeliveryDbContext(options);
await db.Database.EnsureCreatedAsync();

var deliveryRepository = new EfNotificationDeliveryRepository(db);
var preferenceRepository = new InMemoryPreferenceRepository();
var policies = new FixedPolicyProvider();
var inApp = new RecordingDispatcher(NotificationChannel.InApp);
var push = new RecordingDispatcher(NotificationChannel.Push);
var service = new NotificationRuntimeDeliveryService(
    deliveryRepository,
    preferenceRepository,
    policies,
    [inApp, push],
    time);

var createdEvent = PaymentRequestEvent.New(
    Guid.NewGuid(),
    Guid.NewGuid(),
    PaymentRequestEventKind.Created,
    now);
var notification = InAppNotification.New(userId, createdEvent);

await service.DeliverAsync(notification);
Assert(inApp.Calls == 1, "In-App must dispatch by default.");
Assert(push.Calls == 1, "Push must dispatch by default.");
Assert(await deliveryRepository.GetAsync(notification.EventId, NotificationChannel.InApp) is { Status: NotificationDeliveryStatus.Dispatched },
    "In-App delivery must be durable and dispatched.");
Assert(await deliveryRepository.GetAsync(notification.EventId, NotificationChannel.Push) is { Status: NotificationDeliveryStatus.Dispatched },
    "Push delivery must be durable and dispatched.");

await service.DeliverAsync(notification);
Assert(inApp.Calls == 1 && push.Calls == 1, "Replay must not redispatch EventId + Channel identities.");

var pushPolicy = policies.Get(NotificationChannel.Push);
var pushPreference = NotificationPreference.New(userId, pushPolicy, now);
pushPreference.SetEnabled(false, pushPolicy, now.AddSeconds(1));
await preferenceRepository.AddAsync(pushPreference);

var secondEvent = PaymentRequestEvent.New(Guid.NewGuid(), Guid.NewGuid(), PaymentRequestEventKind.Accepted, now.AddMinutes(2));
var second = InAppNotification.New(userId, secondEvent);
await service.DeliverAsync(second);
Assert(inApp.Calls == 2, "In-App must remain enabled.");
Assert(push.Calls == 1, "Disabled Push preference must suppress Push dispatch.");
Assert(await deliveryRepository.GetAsync(second.EventId, NotificationChannel.Push) is null,
    "Disabled channels must not create delivery identities.");

var forcedOffInApp = NotificationPreference.Restore(
    Guid.NewGuid(), userId, NotificationChannel.InApp, false, now, now);
await preferenceRepository.UpsertAsync(forcedOffInApp);
var thirdEvent = PaymentRequestEvent.New(Guid.NewGuid(), Guid.NewGuid(), PaymentRequestEventKind.Declined, now.AddMinutes(3));
await service.DeliverAsync(InAppNotification.New(userId, thirdEvent));
Assert(inApp.Calls == 3, "Non-configurable In-App policy must remain authoritative.");

var noPushService = new NotificationRuntimeDeliveryService(
    deliveryRepository,
    new InMemoryPreferenceRepository(),
    policies,
    [new RecordingDispatcher(NotificationChannel.InApp)],
    time);
var missingDispatcherEvent = PaymentRequestEvent.New(Guid.NewGuid(), Guid.NewGuid(), PaymentRequestEventKind.Cancelled, now.AddMinutes(4));
try
{
    await noPushService.DeliverAsync(InAppNotification.New(Guid.NewGuid(), missingDispatcherEvent));
    throw new InvalidOperationException("Expected missing Push dispatcher failure.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("No dispatcher", StringComparison.Ordinal)) { }

Console.WriteLine("AFW-BE-NOTIFICATION-DELIVERY-1 runtime preference-aware routing scenarios: PASS");

sealed class RecordingDispatcher(NotificationChannel channel) : INotificationChannelDispatchPort
{
    public NotificationChannel Channel { get; } = channel;
    public int Calls { get; private set; }
    public Task DispatchAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Assert(delivery.Channel == Channel, "Dispatcher channel mismatch.");
        Calls++;
        return Task.CompletedTask;
    }
}

sealed class InMemoryPreferenceRepository : INotificationPreferenceRepository
{
    private readonly Dictionary<(Guid, NotificationChannel), NotificationPreference> values = [];

    public Task<NotificationPreference?> GetAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue((userId, channel), out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<NotificationPreference>>(
            values.Values.Where(x => x.UserId == userId).ToArray());
    }

    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add((preference.UserId, preference.Channel), preference);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default) =>
        UpsertAsync(preference, cancellationToken);

    public Task UpsertAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[(preference.UserId, preference.Channel)] = preference;
        return Task.CompletedTask;
    }
}

sealed class FixedPolicyProvider : INotificationChannelPolicyProvider
{
    private readonly NotificationChannelPolicy[] policies =
    [
        NotificationChannelPolicy.Create(NotificationChannel.InApp, true, false),
        NotificationChannelPolicy.Create(NotificationChannel.Push, true, true)
    ];

    public NotificationChannelPolicy Get(NotificationChannel channel) =>
        policies.Single(x => x.Channel == channel);

    public IReadOnlyList<NotificationChannelPolicy> List() => policies;
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
