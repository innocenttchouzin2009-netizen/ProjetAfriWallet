using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var requestId = PaymentRequestId.From(Guid.NewGuid());
var now = new DateTimeOffset(2026, 9, 15, 16, 45, 0, TimeSpan.Zero);
var envelope = new PaymentRequestEventEnvelope(Guid.NewGuid(), requestId, "payment-request.paid", now, "{}");
var store = new SingleEventOutboxStore(envelope, now);
var transport = new CapturingTransport();

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IPaymentRequestEventOutboxStore>(store);
services.AddSingleton<IPaymentRequestEventTransport>(transport);
services.AddScoped<IPaymentRequestEventDeliveryPort, ProviderNeutralPaymentRequestEventDeliveryAdapter>();
services.AddScoped<PaymentRequestEventOutboxProcessor>();
await using var provider = services.BuildServiceProvider();

var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
var workerOptions = new PaymentRequestEventDispatchWorkerOptions(10, TimeSpan.FromHours(1));
var worker1 = new PaymentRequestEventOutboxHostedWorker(
    scopeFactory,
    TimeProvider.System,
    workerOptions,
    loggerFactory.CreateLogger<PaymentRequestEventOutboxHostedWorker>());
var worker2 = new PaymentRequestEventOutboxHostedWorker(
    scopeFactory,
    TimeProvider.System,
    workerOptions,
    loggerFactory.CreateLogger<PaymentRequestEventOutboxHostedWorker>());

await worker1.StartAsync(CancellationToken.None);
await worker2.StartAsync(CancellationToken.None);
await store.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
await worker1.StopAsync(CancellationToken.None);
await worker2.StopAsync(CancellationToken.None);

Assert(transport.DispatchCount == 1, "Only one hosted worker may dispatch in the same process instance.");
Assert(store.MarkDeliveredCount == 1, "Delivered event must be acknowledged exactly once.");

var idleStore = new SingleEventOutboxStore(envelope, now);
var idleServices = new ServiceCollection();
idleServices.AddLogging();
idleServices.AddSingleton<IPaymentRequestEventOutboxStore>(idleStore);
idleServices.AddScoped<IPaymentRequestEventDeliveryPort, ProviderNeutralPaymentRequestEventDeliveryAdapter>();
idleServices.AddScoped<PaymentRequestEventOutboxProcessor>();
await using var idleProvider = idleServices.BuildServiceProvider();
var idleLoggerFactory = idleProvider.GetRequiredService<ILoggerFactory>();
var idleWorker = new PaymentRequestEventOutboxHostedWorker(
    idleProvider.GetRequiredService<IServiceScopeFactory>(),
    TimeProvider.System,
    new PaymentRequestEventDispatchWorkerOptions(10, TimeSpan.FromMilliseconds(20)),
    idleLoggerFactory.CreateLogger<PaymentRequestEventOutboxHostedWorker>());
await idleWorker.StartAsync(CancellationToken.None);
await Task.Delay(60);
await idleWorker.StopAsync(CancellationToken.None);
Assert(idleStore.ClaimCalls == 0, "Worker must remain idle when no transport is registered.");

Console.WriteLine("AFW-BE-REQUEST-EVENTS-1 hosted outbox worker scenarios: PASS");

sealed class CapturingTransport : IPaymentRequestEventTransport
{
    public int DispatchCount { get; private set; }

    public Task DispatchAsync(PaymentRequestEventDispatch dispatch, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DispatchCount++;
        return Task.CompletedTask;
    }
}

sealed class SingleEventOutboxStore(PaymentRequestEventEnvelope envelope, DateTimeOffset nowUtc)
    : IPaymentRequestEventOutboxStore
{
    private int claimed;
    public int ClaimCalls { get; private set; }
    public int MarkDeliveredCount { get; private set; }
    public TaskCompletionSource Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<bool> EnqueueAsync(PaymentRequestEventEnvelope paymentRequestEvent, DateTimeOffset enqueuedAtUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IReadOnlyList<PaymentRequestEventOutboxItem>> ClaimBatchAsync(
        int maxCount,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClaimCalls++;
        if (Interlocked.Exchange(ref claimed, 1) != 0)
        {
            return Task.FromResult<IReadOnlyList<PaymentRequestEventOutboxItem>>([]);
        }

        IReadOnlyList<PaymentRequestEventOutboxItem> result =
        [
            new PaymentRequestEventOutboxItem(
                envelope,
                PaymentRequestEventOutboxStatus.Processing,
                1,
                nowUtc,
                nowUtc,
                nowUtc.AddMinutes(1),
                nowUtc,
                null,
                null)
        ];
        return Task.FromResult(result);
    }

    public Task MarkDeliveredAsync(Guid eventId, DateTimeOffset deliveredAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MarkDeliveredCount++;
        Delivered.TrySetResult();
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid eventId, DateTimeOffset failedAtUtc, string error, DateTimeOffset? nextAttemptAtUtc, bool deadLetter, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<int> RecoverExpiredClaimsAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}
