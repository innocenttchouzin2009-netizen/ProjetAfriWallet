using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Push.Infrastructure;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 16, 9, 30, 0, TimeSpan.Zero);
var userId = Guid.NewGuid();
var android = PushDeviceRegistration.Create(userId, "android-device", PushPlatform.Android, "android-token", now.AddMinutes(-10));
var ios = PushDeviceRegistration.Create(userId, "ios-device", PushPlatform.Ios, "ios-token", now.AddMinutes(-10));
var invalid = PushDeviceRegistration.Create(userId, "invalid-device", PushPlatform.Android, "invalid-token", now.AddMinutes(-10));
var repository = new InMemoryRepository([android, ios, invalid]);
var androidClient = new RecordingAndroidClient();
var iosClient = new RecordingIosClient();
var adapter = new PlatformPushDeliveryAdapter(androidClient, iosClient);
var service = new PushDeliveryOrchestrationService(repository, adapter);
var message = PushNotificationMessage.Create(
    Guid.NewGuid(),
    "Payment request",
    "You received a payment request.",
    new Dictionary<string, string> { ["kind"] = "payment-request" });

var result = await service.DeliverToUserAsync(userId, message, now);
Assert(result.Targets == 3, "Three active targets expected.");
Assert(result.Delivered == 2, "Two deliveries must succeed.");
Assert(result.Failed == 1, "One delivery must fail.");
Assert(result.InvalidTokensDeactivated == 1, "Invalid token must be deactivated.");
Assert(androidClient.Calls == 2, "Android provider must receive Android targets only.");
Assert(iosClient.Calls == 1, "iOS provider must receive iOS targets only.");
Assert(!invalid.IsActive && invalid.DeactivatedAtUtc == now, "Invalid registration must be deactivated at attempt time.");
Assert(repository.UpdateCalls == 1, "Only invalid token must cause repository update.");
Assert(androidClient.LastNotificationId == message.NotificationId, "Notification id must be forwarded.");
Assert(iosClient.LastTitle == message.Title, "Title must be forwarded.");

var empty = await service.DeliverToUserAsync(Guid.NewGuid(), message, now);
Assert(empty.Targets == 0 && empty.Delivered == 0 && empty.Failed == 0, "Unknown user must be a no-op delivery batch.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.DeliverToUserAsync(userId, message, now, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

try
{
    await service.DeliverToUserAsync(Guid.Empty, message, now);
    throw new InvalidOperationException("Expected empty user id validation.");
}
catch (ArgumentException) { }

try
{
    await service.DeliverToUserAsync(userId, message, now.ToOffset(TimeSpan.FromHours(1)));
    throw new InvalidOperationException("Expected UTC validation.");
}
catch (ArgumentException) { }

Console.WriteLine("AFW-BE-NOTIFICATION-PUSH-1 provider adapter and delivery orchestration scenarios: PASS");

sealed class InMemoryRepository(IEnumerable<PushDeviceRegistration> registrations) : IPushDeviceRegistrationRepository
{
    private readonly List<PushDeviceRegistration> values = registrations.ToList();
    public int UpdateCalls { get; private set; }

    public Task<PushDeviceRegistration?> FindByInstallationIdAsync(string installationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.SingleOrDefault(x => x.InstallationId == installationId));
    }

    public Task<IReadOnlyList<PushDeviceRegistration>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PushDeviceRegistration>>(values.Where(x => x.UserId == userId && x.IsActive).ToArray());
    }

    public Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(registration);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

sealed class RecordingAndroidClient : IAndroidPushProviderClient
{
    public int Calls { get; private set; }
    public Guid LastNotificationId { get; private set; }

    public Task<PushProviderResult> SendAsync(PushProviderRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastNotificationId = request.NotificationId;
        return Task.FromResult(request.PushToken == "invalid-token"
            ? PushProviderResult.Failed(PushProviderFailureKind.InvalidToken)
            : PushProviderResult.Delivered("android-provider-id"));
    }
}

sealed class RecordingIosClient : IIosPushProviderClient
{
    public int Calls { get; private set; }
    public string? LastTitle { get; private set; }

    public Task<PushProviderResult> SendAsync(PushProviderRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastTitle = request.Title;
        return Task.FromResult(PushProviderResult.Delivered("ios-provider-id"));
    }
}
