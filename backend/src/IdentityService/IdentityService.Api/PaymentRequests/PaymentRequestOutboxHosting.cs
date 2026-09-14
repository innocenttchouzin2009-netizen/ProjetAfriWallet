using AfriWallet.PaymentRequests.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.PaymentRequests;

public sealed class PaymentRequestOutboxHostingOptions
{
    public const string SectionName = "PaymentRequests:Outbox";

    public bool Enabled { get; init; }
    public int IntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 50;
    public int MaxDeliveryAttempts { get; init; } = 3;

    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);

    public void Validate()
    {
        if (IntervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(IntervalSeconds), "Outbox interval must be positive.");
        }

        if (BatchSize <= 0 || BatchSize > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), "Outbox batch size must be between 1 and 1000.");
        }

        if (MaxDeliveryAttempts <= 0 || MaxDeliveryAttempts > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDeliveryAttempts), "Outbox delivery attempts must be between 1 and 10.");
        }
    }
}

public enum PaymentRequestOutboxCycleStatus
{
    NeverRun = 0,
    Completed = 1,
    SkippedConcurrent = 2,
    TransportUnavailable = 3,
    Failed = 4
}

public sealed record PaymentRequestOutboxCycleResult(
    PaymentRequestOutboxCycleStatus Status,
    int PendingRead,
    int Published,
    int Failed,
    string? Error = null);

public sealed record PaymentRequestOutboxOperationalSnapshot(
    PaymentRequestOutboxCycleStatus LastStatus,
    long CyclesStarted,
    long CyclesCompleted,
    long ConcurrentSkips,
    long TransportUnavailableCycles,
    long FailedCycles,
    DateTimeOffset? LastStartedAtUtc,
    DateTimeOffset? LastCompletedAtUtc,
    int LastPendingRead,
    int LastPublished,
    int LastFailed,
    string? LastError);

public sealed class PaymentRequestOutboxOperationalState
{
    private readonly object sync = new();
    private PaymentRequestOutboxOperationalSnapshot snapshot = new(
        PaymentRequestOutboxCycleStatus.NeverRun,
        0,
        0,
        0,
        0,
        0,
        null,
        null,
        0,
        0,
        0,
        null);

    public PaymentRequestOutboxOperationalSnapshot Snapshot
    {
        get
        {
            lock (sync)
            {
                return snapshot;
            }
        }
    }

    internal void RecordStarted(DateTimeOffset startedAtUtc)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                CyclesStarted = snapshot.CyclesStarted + 1,
                LastStartedAtUtc = startedAtUtc,
                LastError = null
            };
        }
    }

    internal void RecordSkippedConcurrent()
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                LastStatus = PaymentRequestOutboxCycleStatus.SkippedConcurrent,
                ConcurrentSkips = snapshot.ConcurrentSkips + 1
            };
        }
    }

    internal void RecordTransportUnavailable(DateTimeOffset completedAtUtc)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                LastStatus = PaymentRequestOutboxCycleStatus.TransportUnavailable,
                CyclesCompleted = snapshot.CyclesCompleted + 1,
                TransportUnavailableCycles = snapshot.TransportUnavailableCycles + 1,
                LastCompletedAtUtc = completedAtUtc,
                LastPendingRead = 0,
                LastPublished = 0,
                LastFailed = 0,
                LastError = null
            };
        }
    }

    internal void RecordCompleted(PaymentRequestOutboxDispatchResult result, DateTimeOffset completedAtUtc)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                LastStatus = PaymentRequestOutboxCycleStatus.Completed,
                CyclesCompleted = snapshot.CyclesCompleted + 1,
                LastCompletedAtUtc = completedAtUtc,
                LastPendingRead = result.PendingRead,
                LastPublished = result.Published,
                LastFailed = result.Failed,
                LastError = null
            };
        }
    }

    internal void RecordFailed(Exception exception, DateTimeOffset completedAtUtc)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                LastStatus = PaymentRequestOutboxCycleStatus.Failed,
                CyclesCompleted = snapshot.CyclesCompleted + 1,
                FailedCycles = snapshot.FailedCycles + 1,
                LastCompletedAtUtc = completedAtUtc,
                LastError = exception.GetType().Name + ": " + exception.Message
            };
        }
    }
}

public sealed class PaymentRequestOutboxDispatchCoordinator(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    PaymentRequestOutboxHostingOptions options,
    PaymentRequestOutboxOperationalState state,
    ILogger<PaymentRequestOutboxDispatchCoordinator> logger)
{
    private int running;

    public async Task<PaymentRequestOutboxCycleResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
        {
            state.RecordSkippedConcurrent();
            logger.LogWarning("Payment request outbox dispatch skipped because another cycle is already running.");
            return new PaymentRequestOutboxCycleResult(
                PaymentRequestOutboxCycleStatus.SkippedConcurrent,
                0,
                0,
                0);
        }

        var startedAtUtc = timeProvider.GetUtcNow();
        state.RecordStarted(startedAtUtc);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var transport = scope.ServiceProvider.GetService<IPaymentRequestOutboxTransport>();
            if (transport is null)
            {
                var completedAtUtc = timeProvider.GetUtcNow();
                state.RecordTransportUnavailable(completedAtUtc);
                logger.LogWarning(
                    "Payment request outbox dispatch is enabled but no IPaymentRequestOutboxTransport is registered; no messages were read or published.");
                return new PaymentRequestOutboxCycleResult(
                    PaymentRequestOutboxCycleStatus.TransportUnavailable,
                    0,
                    0,
                    0);
            }

            var store = scope.ServiceProvider.GetRequiredService<IPaymentRequestOutboxStore>();
            var dispatcher = new PaymentRequestOutboxDispatcher(
                store,
                transport,
                timeProvider,
                new PaymentRequestOutboxDispatchOptions(options.BatchSize, options.MaxDeliveryAttempts));

            var result = await dispatcher.DispatchAsync(cancellationToken);
            var completedAt = timeProvider.GetUtcNow();
            state.RecordCompleted(result, completedAt);
            logger.LogInformation(
                "Payment request outbox dispatch completed. PendingRead={PendingRead} Published={Published} Failed={Failed} BatchSize={BatchSize}",
                result.PendingRead,
                result.Published,
                result.Failed,
                options.BatchSize);

            return new PaymentRequestOutboxCycleResult(
                PaymentRequestOutboxCycleStatus.Completed,
                result.PendingRead,
                result.Published,
                result.Failed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var completedAtUtc = timeProvider.GetUtcNow();
            state.RecordFailed(exception, completedAtUtc);
            logger.LogError(exception, "Payment request outbox dispatch cycle failed.");
            return new PaymentRequestOutboxCycleResult(
                PaymentRequestOutboxCycleStatus.Failed,
                0,
                0,
                0,
                exception.Message);
        }
        finally
        {
            Volatile.Write(ref running, 0);
        }
    }
}

public sealed class PaymentRequestOutboxHostedService(
    PaymentRequestOutboxDispatchCoordinator coordinator,
    PaymentRequestOutboxHostingOptions options,
    ILogger<PaymentRequestOutboxHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Validate();

        if (!options.Enabled)
        {
            logger.LogInformation("Payment request outbox dispatcher hosting is disabled by configuration.");
            return;
        }

        logger.LogInformation(
            "Payment request outbox dispatcher hosting started. IntervalSeconds={IntervalSeconds} BatchSize={BatchSize} MaxDeliveryAttempts={MaxDeliveryAttempts}",
            options.IntervalSeconds,
            options.BatchSize,
            options.MaxDeliveryAttempts);

        await coordinator.RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(options.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await coordinator.RunOnceAsync(stoppingToken);
        }
    }
}
