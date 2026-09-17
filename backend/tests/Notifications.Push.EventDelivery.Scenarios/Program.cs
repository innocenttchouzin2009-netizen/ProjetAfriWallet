using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static NotificationDeliveryRoutingService CreateRouting(params NotificationPreference[] preferences) =>
    new(new InMemoryPreferenceRepository(preferences), new TestChannelPolicyProvider());

var now = new DateTimeOffset(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);
var userId = Guid.NewGuid();
var firstDevice = PushDeviceRegistration.Create(userId, "installation-a", PushPlatform.Android, "token-a", now.AddMinutes(-5));
var secondDevice = PushDeviceRegistration.Create(userId, "installation-b", PushPlatform.Ios, "token-b", now.AddMinutes(-5));
var deviceRepository = new InMemoryDeviceRepository([firstDevice, secondDevice]);
var deliveryPort = new ScriptedDeliveryPort(new Dictionary<Guid, Queue<PushDeliveryResult>>
{
    [firstDevice.Id.Value] = new Queue<PushDeliveryResult>([PushDeliveryResult.Delivered("m1")]),
    [secondDevice.Id.Value] = new Queue<PushDeliveryResult>([
        PushDeliveryResult.Failed(PushDeliveryFailureKind.Transient),
        PushDeliveryResult.Delivered("m2")])
});
var orchestration = new PushDeliveryOrchestrationService(deviceRepository, deliveryPort);
var routing = CreateRouting();

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-push-event-{Guid.NewGuid():N}.db");
try
{
    var options = new DbContextOptionsBuilder<PushEventDeliveryDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;
    await using var db = new PushEventDeliveryDbContext(options);
    await db.Database.EnsureCreatedAsync();
    var durableRepository = new EfPushEventDeliveryRepository(db);
    var service = new NotificationEventPushDeliveryService(
        orchestration,
        durableRepository,
        PushEventRetryPolicy.Default,
        routing);

    var evt = PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Created, now);
    var notification = InAppNotification.New(userId, evt);

    var first = await service.DeliverAsync(notification, now);
    Assert(first.AttemptedTargets == 2, "First delivery must target both active devices.");
    Assert(first.Delivered == 1 && first.RetryScheduled == 1, "First delivery must persist one success and one transient retry.");
    Assert(deliveryPort.TotalCalls == 2, "Exactly two provider calls expected on first attempt.");

    var replayBeforeDue = await service.DeliverAsync(notification, now.AddSeconds(30));
    Assert(replayBeforeDue.AttemptedTargets == 0, "Replay before retry window must not redeliver any device.");
    Assert(deliveryPort.TotalCalls == 2, "Replay before retry window must not call provider.");

    var retry = await service.DeliverAsync(notification, now.AddMinutes(1));
    Assert(retry.AttemptedTargets == 1 && retry.Delivered == 1, "Retry must target only the transient device.");
    Assert(deliveryPort.TotalCalls == 3, "Only one provider call expected during retry.");
    Assert(deliveryPort.CallsByRegistration[firstDevice.Id.Value] == 1, "Already delivered device must never be redelivered.");
    Assert(deliveryPort.CallsByRegistration[secondDevice.Id.Value] == 2, "Transient device must be retried exactly once.");

    var finalReplay = await service.DeliverAsync(notification, now.AddMinutes(10));
    Assert(finalReplay.AttemptedTargets == 0, "Fully delivered event replay must be idempotent.");
    Assert(deliveryPort.TotalCalls == 3, "Fully delivered event replay must not call provider.");

    var persisted = await durableRepository.ListByEventAsync(evt.EventId);
    Assert(persisted.Count == 2, "One durable delivery row per event/device is required.");
    Assert(persisted.All(x => x.State == PushEventDeliveryState.Delivered), "Both device rows must finish Delivered.");
    Assert(persisted.Single(x => x.RegistrationId == firstDevice.Id).AttemptCount == 1, "Successful device attempt count mismatch.");
    Assert(persisted.Single(x => x.RegistrationId == secondDevice.Id).AttemptCount == 2, "Retried device attempt count mismatch.");

    var permanentDevice = PushDeviceRegistration.Create(userId, "installation-c", PushPlatform.Android, "token-c", now.AddMinutes(-5));
    var permanentRepo = new InMemoryDeviceRepository([permanentDevice]);
    var permanentPort = new ScriptedDeliveryPort(new Dictionary<Guid, Queue<PushDeliveryResult>>
    {
        [permanentDevice.Id.Value] = new Queue<PushDeliveryResult>([PushDeliveryResult.Failed(PushDeliveryFailureKind.Permanent)])
    });
    var permanentService = new NotificationEventPushDeliveryService(
        new PushDeliveryOrchestrationService(permanentRepo, permanentPort),
        durableRepository,
        PushEventRetryPolicy.Default,
        routing);
    var permanentEvent = PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Declined, now.AddMinutes(2));
    var permanentNotification = InAppNotification.New(userId, permanentEvent);
    var permanent = await permanentService.DeliverAsync(permanentNotification, now.AddMinutes(2));
    Assert(permanent.TerminalFailures == 1, "Permanent failure must be terminal.");
    await permanentService.DeliverAsync(permanentNotification, now.AddHours(1));
    Assert(permanentPort.TotalCalls == 1, "Permanent failure must never be retried.");

    var disabledPushPreference = NotificationPreference.Restore(
        Guid.NewGuid(),
        userId,
        NotificationChannel.Push,
        isEnabled: false,
        now.AddMinutes(-10),
        now.AddMinutes(-10));
    var disabledRouting = CreateRouting(disabledPushPreference);

    var inAppDecision = await disabledRouting.EvaluateAsync(userId, NotificationChannel.InApp);
    Assert(inAppDecision.IsEnabled, "Disabling Push must not disable mandatory In-App notifications.");

    var disabledService = new NotificationEventPushDeliveryService(
        new PushDeliveryOrchestrationService(new ThrowingDeviceRepository(), new ThrowingDeliveryPort()),
        new ThrowingEventDeliveryRepository(),
        PushEventRetryPolicy.Default,
        disabledRouting);
    var disabledEvent = PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Accepted, now.AddMinutes(3));
    var disabledNotification = InAppNotification.New(userId, disabledEvent);
    var disabled = await disabledService.DeliverAsync(disabledNotification, now.AddMinutes(3));

    Assert(disabled.AttemptedTargets == 0, "Disabled Push must attempt zero devices.");
    Assert(disabled.Delivered == 0 && disabled.RetryScheduled == 0 && disabled.TerminalFailures == 0,
        "Disabled Push must produce no delivery, retry or failure state.");

    Console.WriteLine("AFW-BE-NOTIFICATION-ROUTING-1 push preference enforcement scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class InMemoryDeviceRepository(IEnumerable<PushDeviceRegistration> registrations) : IPushDeviceRegistrationRepository
{
    private readonly Dictionary<Guid, PushDeviceRegistration> values = registrations.ToDictionary(x => x.Id.Value);

    public Task<PushDeviceRegistration?> FindByInstallationIdAsync(string installationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.SingleOrDefault(x => x.InstallationId == installationId));

    public Task<IReadOnlyList<PushDeviceRegistration>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PushDeviceRegistration>>(values.Values.Where(x => x.UserId == userId && x.IsActive).ToArray());

    public Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        values[registration.Id.Value] = registration;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        values[registration.Id.Value] = registration;
        return Task.CompletedTask;
    }
}

sealed class ScriptedDeliveryPort(Dictionary<Guid, Queue<PushDeliveryResult>> scripts) : IPushDeliveryPort
{
    public Dictionary<Guid, int> CallsByRegistration { get; } = new();
    public int TotalCalls => CallsByRegistration.Values.Sum();

    public Task<PushDeliveryResult> DeliverAsync(PushDeliveryTarget target, PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallsByRegistration[target.RegistrationId.Value] = CallsByRegistration.GetValueOrDefault(target.RegistrationId.Value) + 1;
        if (!scripts.TryGetValue(target.RegistrationId.Value, out var queue) || queue.Count == 0)
            throw new InvalidOperationException("No scripted push result for target.");
        return Task.FromResult(queue.Dequeue());
    }
}

sealed class InMemoryPreferenceRepository(IEnumerable<NotificationPreference> preferences) : INotificationPreferenceRepository
{
    private readonly Dictionary<(Guid UserId, NotificationChannel Channel), NotificationPreference> values =
        preferences.ToDictionary(x => (x.UserId, x.Channel));

    public Task<NotificationPreference?> GetAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue((userId, channel), out var preference);
        return Task.FromResult(preference);
    }

    public Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<NotificationPreference>>(values.Values.Where(x => x.UserId == userId).ToArray());
    }

    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add((preference.UserId, preference.Channel), preference);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[(preference.UserId, preference.Channel)] = preference;
        return Task.CompletedTask;
    }
}

sealed class TestChannelPolicyProvider : INotificationChannelPolicyProvider
{
    private static readonly NotificationChannelPolicy InApp =
        NotificationChannelPolicy.Create(NotificationChannel.InApp, defaultEnabled: true, userConfigurable: false);
    private static readonly NotificationChannelPolicy Push =
        NotificationChannelPolicy.Create(NotificationChannel.Push, defaultEnabled: true, userConfigurable: true);

    public NotificationChannelPolicy Get(NotificationChannel channel) => channel switch
    {
        NotificationChannel.InApp => InApp,
        NotificationChannel.Push => Push,
        _ => throw new ArgumentOutOfRangeException(nameof(channel))
    };

    public IReadOnlyList<NotificationChannelPolicy> List() => [InApp, Push];
}

sealed class ThrowingDeviceRepository : IPushDeviceRegistrationRepository
{
    private static Exception Unexpected() => new InvalidOperationException("Push-disabled flow must not inspect devices.");
    public Task<PushDeviceRegistration?> FindByInstallationIdAsync(string installationId, CancellationToken cancellationToken = default) => throw Unexpected();
    public Task<IReadOnlyList<PushDeviceRegistration>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default) => throw Unexpected();
    public Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default) => throw Unexpected();
    public Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default) => throw Unexpected();
}

sealed class ThrowingDeliveryPort : IPushDeliveryPort
{
    public Task<PushDeliveryResult> DeliverAsync(PushDeliveryTarget target, PushNotificationMessage message, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Push-disabled flow must not call the provider.");
}

sealed class ThrowingEventDeliveryRepository : IPushEventDeliveryRepository
{
    public Task<IReadOnlyList<PushEventDeliveryRecord>> ListByEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Push-disabled flow must not inspect retry delivery state.");

    public Task UpsertAsync(PushEventDeliveryRecord record, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Push-disabled flow must not persist delivery attempts.");
}
