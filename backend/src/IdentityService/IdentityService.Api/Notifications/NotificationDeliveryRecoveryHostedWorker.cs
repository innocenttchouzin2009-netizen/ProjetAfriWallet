using AfriWallet.Notifications.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.Notifications;

public sealed record NotificationDeliveryRecoveryWorkerOptions(
    bool Enabled,
    TimeSpan PollInterval)
{
    private const string ConfigurationPrefix = "Notifications:DeliveryRecovery:Worker";

    public static NotificationDeliveryRecoveryWorkerOptions Default { get; } =
        new(
            Enabled: true,
            PollInterval: TimeSpan.FromSeconds(5));

    public static NotificationDeliveryRecoveryWorkerOptions FromConfiguration(IConfiguration? configuration)
    {
        if (configuration is null)
            return Default;

        var defaults = Default;
        var options = new NotificationDeliveryRecoveryWorkerOptions(
            ReadBool(configuration[$"{ConfigurationPrefix}:Enabled"], defaults.Enabled),
            TimeSpan.FromMilliseconds(ReadInt(
                configuration[$"{ConfigurationPrefix}:PollIntervalMilliseconds"],
                checked((int)defaults.PollInterval.TotalMilliseconds))));

        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (PollInterval < TimeSpan.FromMilliseconds(10))
            throw new InvalidOperationException("Notification recovery worker poll interval must be at least 10 ms.");
    }

    private static bool ReadBool(string? raw, bool fallback) =>
        string.IsNullOrWhiteSpace(raw)
            ? fallback
            : bool.TryParse(raw, out var value)
                ? value
                : throw new InvalidOperationException($"Invalid boolean value '{raw}' in notification recovery configuration.");

    private static int ReadInt(string? raw, int fallback) =>
        string.IsNullOrWhiteSpace(raw)
            ? fallback
            : int.TryParse(raw, out var value)
                ? value
                : throw new InvalidOperationException($"Invalid integer value '{raw}' in notification recovery configuration.");
}

public sealed class NotificationDeliveryRecoveryHostedWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    NotificationDeliveryRecoveryWorkerOptions options,
    ILogger<NotificationDeliveryRecoveryHostedWorker> logger)
    : BackgroundService
{
    private static int activeWorker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Validate();

        if (!options.Enabled)
        {
            logger.LogInformation("Notification delivery recovery worker is disabled by configuration.");
            return;
        }

        if (Interlocked.CompareExchange(ref activeWorker, 1, 0) != 0)
        {
            logger.LogWarning("Notification delivery recovery worker is already active in this process; duplicate worker instance will exit.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var recovery = scope.ServiceProvider.GetRequiredService<NotificationDeliveryRecoveryService>();
                    var result = await recovery.RecoverAsync(stoppingToken);

                    logger.LogDebug(
                        "Notification delivery recovery cycle completed: claimed {Claimed}, dispatched {Dispatched}, failed {Failed}.",
                        result.Claimed,
                        result.Dispatched,
                        result.Failed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Notification delivery recovery cycle failed with {FailureType}.",
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
