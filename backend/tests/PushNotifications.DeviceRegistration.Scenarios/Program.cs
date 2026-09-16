using AfriWallet.PushNotifications.Application;
using AfriWallet.PushNotifications.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message) where TException : Exception
{
    try { action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}

var userId = Guid.NewGuid();
var now = new DateTimeOffset(2026, 9, 16, 16, 0, 0, TimeSpan.Zero);
var repository = new InMemoryPushDeviceRepository();
var service = new PushDeviceRegistrationService(repository);

var created = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId, " phone-1 ", PushPlatform.Android, "token-A", now));
Assert(created.Status == RegisterPushDeviceStatus.Created, "First registration must be created.");
Assert(created.Device.DeviceId == "phone-1", "Device id must be normalized.");
Assert(created.Device.Status == PushDeviceStatus.Active, "New registration must be active.");
Assert(repository.AddCalls == 1 && repository.UpdateCalls == 0, "Creation must add exactly once.");

var refreshed = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId, "phone-1", PushPlatform.Android, "token-B", now.AddMinutes(1)));
Assert(refreshed.Status == RegisterPushDeviceStatus.Refreshed, "Existing device must be refreshed.");
Assert(repository.AddCalls == 1 && repository.UpdateCalls == 1, "Refresh must not create a second registration.");

var stored = await repository.FindByUserAndDeviceAsync(userId, "phone-1");
Assert(stored?.PushToken == "token-B", "Token rotation must replace the previous token.");
stored!.Revoke(now.AddMinutes(2));
await repository.UpdateAsync(stored);
Assert(stored.Status == PushDeviceStatus.Revoked, "Device must be revocable.");

var reactivated = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId, "phone-1", PushPlatform.Ios, "token-C", now.AddMinutes(3)));
Assert(reactivated.Status == RegisterPushDeviceStatus.Refreshed, "Revoked device must reactivate through refresh.");
Assert(reactivated.Device.Status == PushDeviceStatus.Active, "Refresh must reactivate a revoked device.");
Assert(reactivated.Device.Platform == PushPlatform.Ios, "Platform may be refreshed.");

var second = await service.RegisterAsync(new RegisterPushDeviceCommand(
    userId, "tablet-1", PushPlatform.Android, "tablet-token", now.AddMinutes(4)));
Assert(second.Status == RegisterPushDeviceStatus.Created, "Second device must be independently registered.");
var listed = await service.ListAsync(userId);
Assert(listed.Count == 2, "User must be able to have multiple registered devices.");

AssertThrows<ArgumentException>(
    () => PushDeviceRegistration.Register(Guid.Empty, "x", PushPlatform.Android, "t", now),
    "Empty user id must be rejected.");
AssertThrows<ArgumentException>(
    () => PushDeviceRegistration.Register(userId, "x", PushPlatform.Android, " token ", now),
    "Push token surrounding whitespace must be rejected rather than mutated.");
AssertThrows<ArgumentException>(
    () => PushDeviceRegistration.Register(userId, "x", PushPlatform.Android, "t", now.ToOffset(TimeSpan.FromHours(2))),
    "Non-UTC registration time must be rejected.");
AssertThrows<ArgumentException>(
    () => stored.Refresh(PushPlatform.Android, "token-D", now.AddMinutes(1)),
    "Registration timestamps must not move backwards.");
AssertThrows<ArgumentOutOfRangeException>(
    () => PushDeviceRegistration.Register(userId, "x", (PushPlatform)99, "t", now),
    "Unknown platform must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.ListAsync(userId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-PUSH-1 push device registration domain/application scenarios: PASS");

sealed class InMemoryPushDeviceRepository : IPushDeviceRepository
{
    private readonly Dictionary<(Guid UserId, string DeviceId), PushDeviceRegistration> values = new();
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<PushDeviceRegistration?> FindByUserAndDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue((userId, deviceId), out var value);
        return Task.FromResult(value);
    }

    public Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.TryAdd((registration.UserId, registration.DeviceId), registration))
            throw new InvalidOperationException("Duplicate push device registration.");
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[(registration.UserId, registration.DeviceId)] = registration;
        UpdateCalls++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PushDeviceRegistration>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PushDeviceRegistration>>(
            values.Values.Where(x => x.UserId == userId).ToArray());
    }
}
