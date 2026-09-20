using AfriWallet.PaymentRequests.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestEventRecoveryWorkerOptions(
    bool Enabled,
    int BatchSize,
    TimeSpan PollInterval)
{
    private const string ConfigurationPrefix =
        "PaymentRequests:EventOutbox:Recovery";

    public static PaymentRequestEventRecoveryWorkerOptions Default { get; } =
        new(
            Enabled: true,
            BatchSize: 100,
            PollInterval: TimeSpan.FromSeconds(30));

    public static PaymentRequestEventRecoveryWorkerOptions FromConfiguration(
        IConfiguration? configuration)
    {
        if (configuration is null)
            return Default;

        var defaults = Default;
        var options = new PaymentRequestEventRecoveryWorkerOptions(
            ReadBool(
                configuration[$"{ConfigurationPrefix}:Enabled"],
                defaults.Enabled),
            ReadInt(
                configuration[$"{ConfigurationPrefix}:BatchSize"],
                defaults.BatchSize),
            TimeSpan.FromMilliseconds(
                ReadInt(
                    configuration[$"{ConfigurationPrefix}:PollIntervalMilliseconds"],
                    checked((int)defaults.PollInterval.TotalMilliseconds))));

        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (BatchSize is <= 0 or > 1000)
            throw new InvalidOperationException(
                "Payment request recovery worker batch size must be between 1 and 1000.");

        if (PollInterval < TimeSpan.FromMilliseconds(10))
            throw new InvalidOperationException(
                "Payment request recovery worker poll interval must be at least 10 ms.");
    }

    private static bool ReadBool(string? raw, bool fallback) =>
        string.IsNullOrWhiteSpace(raw)
            ? fallback
            : bool.TryParse(raw, out var value)
                ? value
                : throw new InvalidOperationException(
                    $"Invalid boolean value '{raw}' in payment request recovery configuration.");

    private static int ReadInt(string? raw, int fallback) =>
        string.IsNullOrWhiteSpace(raw)
            ? fallback
            : int.TryParse(raw, out var value)
                ? value
                : throw new InvalidOperationException(
                    $"Invalid integer value '{raw}' in payment request recovery configuration.");
}

public sealed class PaymentRequestEventOutboxRecoveryHostedWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    PaymentRequestEventRecoveryWorkerOptions options,
    ILogger<PaymentRequestEventOutboxRecoveryHostedWorker> logger)
    : BackgroundService
{
    private static int activeWorker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Validate();

        if (!options.Enabled)
        {
            logger.LogInformation(
                "Payment request outbox recovery worker is disabled by configuration.");
            return;
        }

        if (Interlocked.CompareExchange(ref activeWorker, 1, 0) != 0)
        {
            logger.LogWarning(
                "Payment request outbox recovery worker is already active in this process; duplicate worker instance will exit.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var recoveryStore =
                        scope.ServiceProvider.GetRequiredService<IPaymentRequestEventRecoveryStore>();
                    var recovered = await recoveryStore.RecoverExpiredClaimsAsync(
                        options.BatchSize,
                        timeProvider.GetUtcNow(),
                        stoppingToken);

                    if (recovered > 0)
                    {
                        logger.LogWarning(
                            "Recovered {RecoveredCount} expired payment request Outbox claim(s) after worker interruption or restart.",
                            recovered);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Payment request outbox recovery cycle failed with {FailureType}.",
                        exception.GetType().Name);
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
