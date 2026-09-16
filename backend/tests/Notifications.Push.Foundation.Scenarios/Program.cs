using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

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

var now = new DateTimeOffset(2026, 9, 16, 8, 45, 0, TimeSpan.Zero);
var userId = Guid.NewGuid();
var repository = new InMemoryPushRepository();
var service = new PushDeviceRegistrationService(repository);

var registered = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId,
    "install-001",
    PushPlatform.Android,
    "token-a",
    now));
Assert(registered.Status == RegisterPushDeviceStatus.Registered, "First registration must be Registered.");
Assert(registered.Registration.IsActive, "New registration must be active.");
Assert(repository.AddCalls == 1, "Repository Add must be called once.");

var repeated = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId,
    "install-001",
    PushPlatform.Android,
    "token-a",
    now.AddMinutes(1)));
Assert(repeated.Status == RegisterPushDeviceStatus.Existing, "Same installation/token must be idempotent.");
Assert(repository.UpdateCalls == 0, "Idempotent replay must not write.");

var rotated = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId,
    "install-001",
    PushPlatform.Android,
    "token-b",
    now.AddMinutes(2)));
Assert(rotated.Status == RegisterPushDeviceStatus.TokenRotated, "Changed token must rotate.");
Assert(rotated.Registration.PushToken == "token-b", "Rotated token must be preserved.");
Assert(repository.UpdateCalls == 1, "Token rotation must update once.");

await AssertThrowsAsync<InvalidOperationException>(
    () => service.RegisterAsync(new RegisterPushDeviceCommand(
        Guid.NewGuid(), "install-001", PushPlatform.Android, "token-c", now.AddMinutes(3))),
    "Another user must not claim an existing installation.");

await AssertThrowsAsync<InvalidOperationException>(
    () => service.RegisterAsync(new RegisterPushDeviceCommand(
        userId, "install-001", PushPlatform.Ios, "token-c", now.AddMinutes(3))),
    "Installation platform must not change.");

var unregistered = await service.UnregisterAsync(userId, "install-001", now.AddMinutes(4));
Assert(unregistered, "Owned active installation must unregister.");
Assert(repository.UpdateCalls == 2, "Unregister must persist update.");

var reactivated = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId,
    "install-001",
    PushPlatform.Android,
    "token-d",
    now.AddMinutes(5)));
Assert(reactivated.Status == RegisterPushDeviceStatus.Reactivated, "Inactive registration must reactivate.");
Assert(reactivated.Registration.IsActive && reactivated.Registration.DeactivatedAtUtc is null,
    "Reactivated registration must clear deactivation timestamp.");

var active = await repository.ListActiveByUserAsync(userId);
Assert(active.Count == 1 && active[0].PushToken == "token-d", "Active listing must return reactivated device.");

var message = PushNotificationMessage.Create(
    Guid.NewGuid(),
    "Payment request",
    "A payment request is waiting for you.",
    new Dictionary<string, string> { ["type"] = "payment-request.created" });
var target = new PushDeliveryTarget(
    reactivated.Registration.Id,
    userId,
    PushPlatform.Android,
    reactivated.Registration.PushToken);
var deliveryPort = new RecordingDeliveryPort();
var delivery = await deliveryPort.DeliverAsync(target, message);
Assert(delivery.Accepted, "Fake delivery must report accepted.");
Assert(deliveryPort.LastTarget == target, "Delivery port must receive exact target.");
Assert(deliveryPort.LastMessage == message, "Delivery port must receive exact provider-agnostic message.");

using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.RegisterAsync(new RegisterPushDeviceCommand(
        Guid.NewGuid(), "install-cancelled", PushPlatform.Android, "token-z", now), cancelled.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-NOTIFICATION-PUSH-1 device registration and delivery port scenarios: PASS");

sealed class InMemoryPushRepository : IPushDeviceRegistrationRepository
{
    private readonly Dictionary<string, PushDeviceRegistration> registrations = new(StringComparer.Ordinal);
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<PushDeviceRegistration?> FindByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        registrations.TryGetValue(installationId.Trim(), out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<PushDeviceRegistration>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<PushDeviceRegistration> values = registrations.Values
            .Where(x => x.UserId == userId && x.IsActive)
            .ToArray();
        return Task.FromResult(values);
    }

    public Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        registrations.Add(registration.InstallationId, registration);
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        registrations[registration.InstallationId] = registration;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

sealed class RecordingDeliveryPort : IPushDeliveryPort
{
    public PushDeliveryTarget? LastTarget { get; private set; }
    public PushNotificationMessage? LastMessage { get; private set; }

    public Task<PushDeliveryResult> DeliverAsync(
        PushDeliveryTarget target,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastTarget = target;
        LastMessage = message;
        return Task.FromResult(PushDeliveryResult.Delivered("provider-message-1"));
    }
}
