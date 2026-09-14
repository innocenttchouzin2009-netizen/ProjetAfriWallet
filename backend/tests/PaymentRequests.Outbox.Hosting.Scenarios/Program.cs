using AfriWallet.PaymentRequests.Application;
using IdentityService.Api.PaymentRequests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static ServiceProvider BuildProvider(
    PaymentRequestOutboxHostingOptions options,
    RecordingStore store,
    IPaymentRequestOutboxTransport? transport = null)
{
    var services = new ServiceCollection();
    services.AddSingleton(TimeProvider.System);
    services.AddSingleton(options);
    services.AddSingleton<PaymentRequestOutboxOperationalState>();
    services.AddSingleton<IPaymentRequestOutboxStore>(store);
    if (transport is not null)
    {
        services.AddSingleton(transport);
    }
    services.AddLogging();
    services.AddSingleton<PaymentRequestOutboxDispatchCoordinator>();
    return services.BuildServiceProvider();
}

var sampleMessage = new PaymentRequestOutboxDeliveryMessage(
    Guid.NewGuid(),
    Guid.NewGuid(),
    "payment-request.paid",
    "{\"status\":\"Paid\"}",
    DateTimeOffset.UtcNow);

var validOptions = new PaymentRequestOutboxHostingOptions
{
    Enabled = true,
    IntervalSeconds = 30,
    BatchSize = 7,
    MaxDeliveryAttempts = 2
};
validOptions.Validate();

try
{
    new PaymentRequestOutboxHostingOptions { IntervalSeconds = 0 }.Validate();
    throw new InvalidOperationException("Expected invalid interval validation.");
}
catch (ArgumentOutOfRangeException) { }

try
{
    new PaymentRequestOutboxHostingOptions { BatchSize = 1001 }.Validate();
    throw new InvalidOperationException("Expected invalid batch validation.");
}
catch (ArgumentOutOfRangeException) { }

var unavailableStore = new RecordingStore([sampleMessage]);
await using (var provider = BuildProvider(validOptions, unavailableStore))
{
    var coordinator = provider.GetRequiredService<PaymentRequestOutboxDispatchCoordinator>();
    var result = await coordinator.RunOnceAsync();
    Assert(result.Status == PaymentRequestOutboxCycleStatus.TransportUnavailable,
        "Missing transport must produce TransportUnavailable.");
    Assert(unavailableStore.ReadCalls == 0,
        "Missing transport must not read or publish outbox messages.");

    var snapshot = provider.GetRequiredService<PaymentRequestOutboxOperationalState>().Snapshot;
    Assert(snapshot.TransportUnavailableCycles == 1,
        "Operational state must count unavailable transport cycles.");
}

var successStore = new RecordingStore([sampleMessage]);
var successTransport = new RecordingTransport();
await using (var provider = BuildProvider(validOptions, successStore, successTransport))
{
    var coordinator = provider.GetRequiredService<PaymentRequestOutboxDispatchCoordinator>();
    var result = await coordinator.RunOnceAsync();
    Assert(result.Status == PaymentRequestOutboxCycleStatus.Completed,
        "Successful dispatch must complete.");
    Assert(result.PendingRead == 1 && result.Published == 1 && result.Failed == 0,
        "Successful dispatch counters mismatch.");
    Assert(successStore.LastMaxCount == validOptions.BatchSize,
        "Configured batch size must be forwarded to the store.");
    Assert(successTransport.Deliveries == 1,
        "Transport must receive one delivery.");
    Assert(successStore.MarkPublishedCalls == 1,
        "Delivered message must be marked published exactly once.");

    var snapshot = provider.GetRequiredService<PaymentRequestOutboxOperationalState>().Snapshot;
    Assert(snapshot.LastStatus == PaymentRequestOutboxCycleStatus.Completed,
        "Operational state must expose completed status.");
    Assert(snapshot.LastPublished == 1 && snapshot.CyclesStarted == 1 && snapshot.CyclesCompleted == 1,
        "Operational state counters mismatch.");
}

var blockingStore = new RecordingStore([sampleMessage]);
var blockingTransport = new BlockingTransport();
await using (var provider = BuildProvider(validOptions, blockingStore, blockingTransport))
{
    var coordinator = provider.GetRequiredService<PaymentRequestOutboxDispatchCoordinator>();
    var first = coordinator.RunOnceAsync();
    await blockingTransport.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    var second = await coordinator.RunOnceAsync();
    Assert(second.Status == PaymentRequestOutboxCycleStatus.SkippedConcurrent,
        "Concurrent cycle must be skipped.");

    blockingTransport.Release.TrySetResult(true);
    var firstResult = await first;
    Assert(firstResult.Status == PaymentRequestOutboxCycleStatus.Completed,
        "First concurrent cycle must still complete.");

    var snapshot = provider.GetRequiredService<PaymentRequestOutboxOperationalState>().Snapshot;
    Assert(snapshot.ConcurrentSkips == 1,
        "Operational state must count concurrent skips.");
}

var disabledOptions = new PaymentRequestOutboxHostingOptions
{
    Enabled = false,
    IntervalSeconds = 1,
    BatchSize = 5,
    MaxDeliveryAttempts = 1
};
var disabledStore = new RecordingStore([sampleMessage]);
var disabledTransport = new RecordingTransport();
await using (var provider = BuildProvider(disabledOptions, disabledStore, disabledTransport))
{
    var coordinator = provider.GetRequiredService<PaymentRequestOutboxDispatchCoordinator>();
    var hosted = new PaymentRequestOutboxHostedService(
        coordinator,
        disabledOptions,
        NullLogger<PaymentRequestOutboxHostedService>.Instance);

    await hosted.StartAsync(CancellationToken.None);
    await hosted.StopAsync(CancellationToken.None);

    Assert(disabledStore.ReadCalls == 0,
        "Disabled hosting must not dispatch.");
}

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PaymentRequests:Outbox:Enabled"] = "true",
        ["PaymentRequests:Outbox:IntervalSeconds"] = "15",
        ["PaymentRequests:Outbox:BatchSize"] = "25",
        ["PaymentRequests:Outbox:MaxDeliveryAttempts"] = "4"
    })
    .Build();

var registrationServices = new ServiceCollection();
registrationServices.AddLogging();
registrationServices.AddPaymentRequestOutboxHosting(configuration);
await using (var provider = registrationServices.BuildServiceProvider())
{
    var options = provider.GetRequiredService<PaymentRequestOutboxHostingOptions>();
    Assert(options.Enabled && options.IntervalSeconds == 15 && options.BatchSize == 25 && options.MaxDeliveryAttempts == 4,
        "Hosting options must bind from configuration.");
    Assert(provider.GetServices<IHostedService>().Any(service => service is PaymentRequestOutboxHostedService),
        "Outbox hosted service must be registered.");
}

Console.WriteLine("AFW-BE-REQUEST-1 outbox hosting and operational control scenarios: PASS");

sealed class RecordingStore(IReadOnlyList<PaymentRequestOutboxDeliveryMessage> messages) : IPaymentRequestOutboxStore
{
    public int ReadCalls { get; private set; }
    public int MarkPublishedCalls { get; private set; }
    public int LastMaxCount { get; private set; }

    public Task<IReadOnlyList<PaymentRequestOutboxDeliveryMessage>> ReadPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCalls++;
        LastMaxCount = maxCount;
        return Task.FromResult<IReadOnlyList<PaymentRequestOutboxDeliveryMessage>>(messages.Take(maxCount).ToArray());
    }

    public Task MarkPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MarkPublishedCalls++;
        return Task.CompletedTask;
    }
}

sealed class RecordingTransport : IPaymentRequestOutboxTransport
{
    public int Deliveries { get; private set; }

    public Task DeliverAsync(
        PaymentRequestOutboxDeliveryMessage message,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Deliveries++;
        return Task.CompletedTask;
    }
}

sealed class BlockingTransport : IPaymentRequestOutboxTransport
{
    public TaskCompletionSource<bool> Entered { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task DeliverAsync(
        PaymentRequestOutboxDeliveryMessage message,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        Entered.TrySetResult(true);
        await Release.Task.WaitAsync(cancellationToken);
    }
}
