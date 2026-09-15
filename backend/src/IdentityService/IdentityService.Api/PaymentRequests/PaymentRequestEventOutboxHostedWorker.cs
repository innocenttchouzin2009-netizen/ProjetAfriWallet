using AfriWallet.PaymentRequests.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestEventDispatchWorkerOptions(int BatchSize, TimeSpan PollInterval)
{
    public static PaymentRequestEventDispatchWorkerOptions Default { get; } =
        new(50, TimeSpan.FromSeconds(5));

    public void Validate()
    {
        if (BatchSize <= 0) throw new InvalidOperationException("Payment request outbox worker batch size must be positive.");
        if (PollInterval <= TimeSpan.Zero) throw new InvalidOperationException("Payment request outbox worker poll interval must be positive.");
    }
}

public sealed class PaymentRequestEventOutboxHostedWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    PaymentRequestEventDispatchWorkerOptions options,
    PaymentRequestEventOutboxWorkerState state,
    ILogger<PaymentRequestEventOutboxHostedWorker> logger)
    : BackgroundService
{
    private static int activeWorker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Validate();

        if (Interlocked.CompareExchange(ref activeWorker, 1, 0) != 0)
        {
            logger.LogWarning("Payment request outbox dispatch worker is already active in this process; duplicate worker instance will exit.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var transport = scope.ServiceProvider.GetService<IPaymentRequestEventTransport>();
                    if (transport is null)
                    {
                        state.MarkTransportUnavailable("TransportUnavailable");
                        logger.LogDebug("No IPaymentRequestEventTransport is registered; payment request outbox dispatch is idle.");
                    }
                    else
                    {
                        var startedAt = timeProvider.GetUtcNow();
                        state.MarkCycleStarted(startedAt);
                        var processor = scope.ServiceProvider.GetRequiredService<PaymentRequestEventOutboxProcessor>();
                        var delivered = await processor.ProcessBatchAsync(options.BatchSize, startedAt, stoppingToken);
                        state.MarkCycleSucceeded(timeProvider.GetUtcNow(), delivered);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    state.MarkCycleFailed(timeProvider.GetUtcNow(), exception.GetType().Name);
                    logger.LogError("Payment request outbox dispatch cycle failed with {FailureType}.", exception.GetType().Name);
                }

                try
                {
                    await Task.Delay(options.PollInterval, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            Volatile.Write(ref activeWorker, 0);
        }
    }
}
