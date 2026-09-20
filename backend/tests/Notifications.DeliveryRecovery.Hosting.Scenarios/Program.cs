using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using IdentityService.Api.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Notifications:DeliveryRecovery:Worker:Enabled"] = "true",
        ["Notifications:DeliveryRecovery:Worker:PollIntervalMilliseconds"] = "20"
    })
    .Build();

var options = NotificationDeliveryRecoveryWorkerOptions.FromConfiguration(configuration);
Assert(options.Enabled, "Worker must be enabled from configuration.");
Assert(options.PollInterval == TimeSpan.FromMilliseconds(20),
    "Worker poll interval must be loaded from configuration.");

try
{
    NotificationDeliveryRecoveryWorkerOptions.FromConfiguration(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:DeliveryRecovery:Worker:PollIntervalMilliseconds"] = "1"
            })
            .Build());
    throw new InvalidOperationException("Expected invalid poll interval.");
}
catch (InvalidOperationException) { }

var repository = new CountingRepository();
var dispatcher = new RecordingDispatcher(NotificationChannel.InApp);
var services = new ServiceCollection();
services.AddSingleton<INotificationDeliveryRepository>(repository);
services.AddSingleton<INotificationChannelDispatchPort>(dispatcher);
services.AddSingleton(TimeProvider.System);
services.AddSingleton(NotificationDeliveryRecoveryOptions.Default);
services.AddScoped<NotificationDeliveryRecoveryService>();
await using var provider = services.BuildServiceProvider();

var worker = new NotificationDeliveryRecoveryHostedWorker(
    provider.GetRequiredService<IServiceScopeFactory>(),
    TimeProvider.System,
    new NotificationDeliveryRecoveryWorkerOptions(true, TimeSpan.FromMilliseconds(20)),
    NullLogger<NotificationDeliveryRecoveryHostedWorker>.Instance);

await worker.StartAsync(CancellationToken.None);
await Task.Delay(80);
await worker.StopAsync(CancellationToken.None);
Assert(repository.ClaimCalls >= 1, "Hosted worker must execute at least one recovery cycle.");

var disabledRepository = new CountingRepository();
var disabledServices = new ServiceCollection();
disabledServices.AddSingleton<INotificationDeliveryRepository>(disabledRepository);
disabledServices.AddSingleton<INotificationChannelDispatchPort>(dispatcher);
disabledServices.AddSingleton(TimeProvider.System);
disabledServices.AddSingleton(NotificationDeliveryRecoveryOptions.Default);
disabledServices.AddScoped<NotificationDeliveryRecoveryService>();
await using var disabledProvider = disabledServices.BuildServiceProvider();

var disabledWorker = new NotificationDeliveryRecoveryHostedWorker(
    disabledProvider.GetRequiredService<IServiceScopeFactory>(),
    TimeProvider.System,
    new NotificationDeliveryRecoveryWorkerOptions(false, TimeSpan.FromMilliseconds(20)),
    NullLogger<NotificationDeliveryRecoveryHostedWorker>.Instance);

await disabledWorker.StartAsync(CancellationToken.None);
await Task.Delay(40);
await disabledWorker.StopAsync(CancellationToken.None);
Assert(disabledRepository.ClaimCalls == 0, "Disabled worker must not execute recovery cycles.");

var failingRepository = new FailOnceRepository();
var failingServices = new ServiceCollection();
failingServices.AddSingleton<INotificationDeliveryRepository>(failingRepository);
failingServices.AddSingleton<INotificationChannelDispatchPort>(dispatcher);
failingServices.AddSingleton(TimeProvider.System);
failingServices.AddSingleton(NotificationDeliveryRecoveryOptions.Default);
failingServices.AddScoped<NotificationDeliveryRecoveryService>();
await using var failingProvider = failingServices.BuildServiceProvider();

var resilientWorker = new NotificationDeliveryRecoveryHostedWorker(
    failingProvider.GetRequiredService<IServiceScopeFactory>(),
    TimeProvider.System,
    new NotificationDeliveryRecoveryWorkerOptions(true, TimeSpan.FromMilliseconds(20)),
    NullLogger<NotificationDeliveryRecoveryHostedWorker>.Instance);

await resilientWorker.StartAsync(CancellationToken.None);
await Task.Delay(100);
await resilientWorker.StopAsync(CancellationToken.None);
Assert(failingRepository.ClaimCalls >= 2,
    "A failed recovery cycle must not terminate the hosted worker.");

Console.WriteLine("AFW-BE-NOTIFICATION-DELIVERY-RECOVERY-HOSTING-1 scenarios: PASS");

sealed class CountingRepository : INotificationDeliveryRepository
{
    public int ClaimCalls { get; protected set; }

    public Task<NotificationDelivery?> GetAsync(Guid eventId, NotificationChannel channel, Guid recipientUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult<NotificationDelivery?>(null);

    public Task<NotificationDelivery> GetOrAddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default) =>
        Task.FromResult(delivery);

    public virtual Task<IReadOnlyList<NotificationDelivery>> ClaimRecoverableAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset leaseUntilUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClaimCalls++;
        return Task.FromResult<IReadOnlyList<NotificationDelivery>>([]);
    }

    public Task ReleaseRecoveryClaimAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task MarkDispatchedAsync(Guid deliveryId, DateTimeOffset dispatchedAtUtc, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

sealed class FailOnceRepository : CountingRepository
{
    public override Task<IReadOnlyList<NotificationDelivery>> ClaimRecoverableAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset leaseUntilUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ClaimCalls++;
        if (ClaimCalls == 1)
            throw new InvalidOperationException("Simulated cycle failure.");
        return Task.FromResult<IReadOnlyList<NotificationDelivery>>([]);
    }
}

sealed class RecordingDispatcher(NotificationChannel channel) : INotificationChannelDispatchPort
{
    public NotificationChannel Channel { get; } = channel;
    public Task DispatchAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
