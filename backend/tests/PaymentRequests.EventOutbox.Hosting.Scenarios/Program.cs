using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await RunEnabledDispatcherScenarioAsync();
await RunDisabledDispatcherScenarioAsync();
await RunMissingDeliveryPortScenarioAsync();
RunConfigurationValidationScenario();

Console.WriteLine("AFW-BE-REQUEST-EVENT-OUTBOX-HOSTING-1 hosted dispatcher scenarios: PASS");

static async Task RunEnabledDispatcherScenarioAsync()
{
    var store = new FakeOutboxStore();
    var delivery = new RecordingDeliveryPort();
    using var host = BuildHost(enabled: true, store, delivery);

    await host.StartAsync();
    var completed = await Task.WhenAny(store.Delivered, Task.Delay(TimeSpan.FromSeconds(2)));
    Assert(completed == store.Delivered, "Enabled dispatcher must deliver a pending outbox event.");

    var state = host.Services.GetRequiredService<PaymentRequestEventOutboxDispatcherState>().Snapshot;
    Assert(delivery.Calls == 1, "Delivery port must be called exactly once.");
    Assert(store.ClaimCalls >= 1, "Dispatcher must claim an outbox batch.");
    Assert(state.TotalDeliveredCount == 1, "Operational state must record delivered events.");
    Assert(state.Status is PaymentRequestEventOutboxDispatcherStatus.Idle or PaymentRequestEventOutboxDispatcherStatus.Running,
        "Successful dispatcher must remain operational.");

    await host.StopAsync();
}

static async Task RunDisabledDispatcherScenarioAsync()
{
    var store = new FakeOutboxStore();
    var delivery = new RecordingDeliveryPort();
    using var host = BuildHost(enabled: false, store, delivery);

    await host.StartAsync();
    await Task.Delay(100);

    var state = host.Services.GetRequiredService<PaymentRequestEventOutboxDispatcherState>().Snapshot;
    Assert(state.Status == PaymentRequestEventOutboxDispatcherStatus.Disabled,
        "Disabled configuration must keep dispatcher disabled.");
    Assert(store.ClaimCalls == 0, "Disabled dispatcher must never claim outbox events.");
    Assert(delivery.Calls == 0, "Disabled dispatcher must never invoke delivery.");

    await host.StopAsync();
}

static async Task RunMissingDeliveryPortScenarioAsync()
{
    var store = new FakeOutboxStore();
    using var host = BuildHost(enabled: true, store, delivery: null);

    await host.StartAsync();
    await Task.Delay(120);

    var state = host.Services.GetRequiredService<PaymentRequestEventOutboxDispatcherState>().Snapshot;
    Assert(state.Status == PaymentRequestEventOutboxDispatcherStatus.Blocked,
        "Enabled dispatcher without authoritative delivery port must be blocked.");
    Assert(store.ClaimCalls == 0,
        "Dispatcher must not claim events when no delivery port is configured.");
    Assert(state.LastError?.Contains("delivery port", StringComparison.OrdinalIgnoreCase) == true,
        "Blocked state must explain the missing delivery port.");

    await host.StopAsync();
}

static void RunConfigurationValidationScenario()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PaymentRequests:EventOutbox:Dispatcher:BatchSize"] = "0"
        })
        .Build();

    try
    {
        PaymentRequestEventOutboxHostingOptions.FromConfiguration(configuration);
        throw new InvalidOperationException("Expected invalid BatchSize to be rejected.");
    }
    catch (InvalidOperationException exception) when (exception.Message.Contains("BatchSize", StringComparison.Ordinal))
    {
    }
}

static IHost BuildHost(bool enabled, FakeOutboxStore store, RecordingDeliveryPort? delivery)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PaymentRequests:EventOutbox:Dispatcher:Enabled"] = enabled.ToString(),
        ["PaymentRequests:EventOutbox:Dispatcher:BatchSize"] = "10",
        ["PaymentRequests:EventOutbox:Dispatcher:PollIntervalMilliseconds"] = "25",
        ["PaymentRequests:EventOutbox:Dispatcher:MaxAttempts"] = "3",
        ["PaymentRequests:EventOutbox:Dispatcher:LeaseSeconds"] = "30",
        ["PaymentRequests:EventOutbox:Dispatcher:BaseRetryDelaySeconds"] = "1"
    });

    builder.Services.AddPaymentRequestEventOutboxHosting(builder.Configuration);
    builder.Services.AddSingleton<IPaymentRequestEventOutboxStore>(store);
    if (delivery is not null)
    {
        builder.Services.AddSingleton<IPaymentRequestEventDeliveryPort>(delivery);
    }

    return builder.Build();
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class RecordingDeliveryPort : IPaymentRequestEventDeliveryPort
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);

    public Task DeliverAsync(PaymentRequestEventEnvelope paymentRequestEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref calls);
        return Task.CompletedTask;
    }
}

sealed class FakeOutboxStore : IPaymentRequestEventOutboxStore
{
    private readonly PaymentRequestEventOutboxItem item = new(
        new PaymentRequestEventEnvelope(
            Guid.NewGuid(),
            PaymentRequestId.From(Guid.NewGuid()),
            "payment-request.created",
            DateTimeOffset.UtcNow,
            "{}"),
        PaymentRequestEventOutboxStatus.Pending,
        1,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        null,
        null,
        null,
        null);

    private int claimed;
    private int claimCalls;
    private readonly TaskCompletionSource<bool> delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int ClaimCalls => Volatile.Read(ref claimCalls);
    public Task Delivered => delivered.Task;

    public Task<bool> EnqueueAsync(PaymentRequestEventEnvelope paymentRequestEvent, DateTimeOffset enqueuedAtUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IReadOnlyList<PaymentRequestEventOutboxItem>> ClaimBatchAsync(
        int maxCount,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref claimCalls);
        if (Interlocked.Exchange(ref claimed, 1) == 0)
        {
            return Task.FromResult<IReadOnlyList<PaymentRequestEventOutboxItem>>([item]);
        }

        return Task.FromResult<IReadOnlyList<PaymentRequestEventOutboxItem>>([]);
    }

    public Task MarkDeliveredAsync(Guid eventId, DateTimeOffset deliveredAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        delivered.TrySetResult(true);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(
        Guid eventId,
        DateTimeOffset failedAtUtc,
        string error,
        DateTimeOffset? nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<int> RecoverExpiredClaimsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}
