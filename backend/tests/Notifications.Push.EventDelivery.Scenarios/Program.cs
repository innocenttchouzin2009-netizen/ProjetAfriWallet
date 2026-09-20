using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

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

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-push-event-{Guid.NewGuid():N}.db");
try
{
    var options = new DbContextOptionsBuilder<PushEventDeliveryDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;
    await using var db = new PushEventDeliveryDbContext(options);
    await db.Database.EnsureCreatedAsync();
    var durableRepository = new EfPushEventDeliveryRepository(db);
    var service = new NotificationEventPushDeliveryService(orchestration, durableRepository, PushEventRetryPolicy.Default);

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
        PushEventRetryPolicy.Default);
    var permanentEvent = PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Declined, now.AddMinutes(2));
    var permanentNotification = InAppNotification.New(userId, permanentEvent);
    var permanent = await permanentService.DeliverAsync(permanentNotification, now.AddMinutes(2));
    Assert(permanent.TerminalFailures == 1, "Permanent failure must be terminal.");
    await permanentService.DeliverAsync(permanentNotification, now.AddHours(1));
    Assert(permanentPort.TotalCalls == 1, "Permanent failure must never be retried.");

    Console.WriteLine("AFW-BE-NOTIFICATION-PUSH-1 event delivery retry/idempotency scenarios: PASS");
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
