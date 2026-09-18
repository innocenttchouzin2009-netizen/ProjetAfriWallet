using AfriWallet.PaymentRequests.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestEventDispatchWorkerOptions(
    bool Enabled,
    int BatchSize,
    TimeSpan PollInterval,
    int MaxAttempts,
    TimeSpan LeaseDuration,
    TimeSpan BaseRetryDelay)
{
    private const string ConfigurationPrefix = "PaymentRequests:EventOutbox:Dispatcher";

    public static PaymentRequestEventDispatchWorkerOptions Default { get; } =
        new(
            Enabled: true,
            BatchSize: 50,
            PollInterval: TimeSpan.FromSeconds(5),
            MaxAttempts: 5,
            LeaseDuration: TimeSpan.FromMinutes(5),
            BaseRetryDelay: TimeSpan.FromSeconds(30));

    public static PaymentRequestEventDispatchWorkerOptions FromConfiguration(IConfiguration? configuration)
    {
        if (configuration is null)
        {
            return Default;
        }

        var defaults = Default;
        var options = new PaymentRequestEventDispatchWorkerOptions(
            ReadBool(configuration[$"{ConfigurationPrefix}:Enabled"], defaults.Enabled),
            ReadInt(configuration[$"{ConfigurationPrefix}:BatchSize"], defaults.BatchSize),
            TimeSpan.FromMilliseconds(ReadInt(
                configuration[$"{ConfigurationPrefix}:PollIntervalMilliseconds"],
                checked((int)defaults.PollInterval.TotalMilliseconds))),
            ReadInt(configuration[$"{ConfigurationPrefix}:MaxAttempts"], defaults.MaxAttempts),
            TimeSpan.FromSeconds(ReadInt(
                configuration[$"{ConfigurationPrefix}:LeaseSeconds"],
                checked((int)defaults.LeaseDuration.TotalSeconds))),
            TimeSpan.FromSeconds(ReadInt(
                configuration[$"{ConfigurationPrefix}:BaseRetryDelaySeconds"],
                checked((int)defaults.BaseRetryDelay.TotalSeconds))));

        options.Validate();
        return options;
    }

    public PaymentRequestEventDeliveryOptions ToDeliveryOptions() =>
        new(MaxAttempts, LeaseDuration, BaseRetryDelay);

    public void Validate()
    {
        if (BatchSize is <= 0 or > 1000)
        {
            throw new InvalidOperationException("Payment request outbox worker batch size must be between 1 and 1000.");
        }

        if (PollInterval < TimeSpan.FromMilliseconds(10))
        {
            throw new InvalidOperationException("Payment request outbox worker poll interval must be at least 10 ms.");
        }

        if (MaxAttempts is <= 0 or > 100)
        {
            throw new InvalidOperationException("Payment request outbox max attempts must be between 1 and 100.");
        }

        if (LeaseDuration < TimeSpan.FromSeconds(1))
        {
            throw new InvalidOperationException("Payment request outbox lease duration must be at least 1 second.");
        }

        if (BaseRetryDelay < TimeSpan.FromSeconds(1))
        {
            throw new InvalidOperationException("Payment request outbox base retry delay must be at least 1 second.");
        }
    }

    private static bool ReadBool(string? raw, bool fallback) =>
        string.IsNullOrWhiteSpace(raw)
            ? fallback
            : bool.TryParse(raw, out var value)
                ? value
                : throw new InvalidOperationException($"Invalid boolean value '{raw}' in payment request outbox configuration.");

    private static int ReadInt(string? raw, int fallback) =>
        string.IsNullOrWhiteSpace(raw)
            ? fallback
            : int.TryParse(raw, out var value)
                ? value
                : throw new InvalidOperationException($"Invalid integer value '{raw}' in payment request outbox configuration.");
}

public sealed class PaymentRequestEventOutboxHostedWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    PaymentRequestEventDispatchWorkerOptions options,
    ILogger<PaymentRequestEventOutboxHostedWorker> logger,
    PaymentRequestEventOutboxWorkerState? observabilityState = null)
    : BackgroundService
{
    private static int activeWorker;
    private readonly PaymentRequestEventOutboxWorkerState state = observabilityState ?? new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Validate();

        if (!options.Enabled)
        {
            state.MarkDisabled();
            logger.LogInformation("Payment request outbox dispatch worker is disabled by configuration.");
            return;
        }

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
