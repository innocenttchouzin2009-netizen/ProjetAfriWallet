using AfriWallet.PaymentRequests.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestEventOutboxOperationalSnapshot(
    PaymentRequestEventOutboxDispatcherStatus Status,
    bool Ready,
    string ReadinessReason,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    int LastDeliveredCount,
    long TotalDeliveredCount,
    long TotalCycleCount,
    long TotalFailedCycleCount,
    int ConsecutiveFailures,
    string? LastError,
    long PendingCount,
    long ProcessingCount,
    long RetryCount,
    long DeliveredCount,
    long DeadLetterCount,
    DateTimeOffset? OldestUndeliveredAtUtc,
    DateTimeOffset? LatestDeadLetterAtUtc);

public sealed class PaymentRequestEventOutboxOperationalHealthService(
    PaymentRequestEventOutboxDispatcherState dispatcherState,
    PaymentRequestEventOutboxHostingOptions options,
    IPaymentRequestEventOutboxDiagnostics diagnostics,
    IServiceProvider serviceProvider)
{
    public async Task<PaymentRequestEventOutboxOperationalSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = dispatcherState.Snapshot;
        var store = await diagnostics.GetSnapshotAsync(cancellationToken);
        var deliveryConfigured = serviceProvider.GetService<IPaymentRequestEventDeliveryPort>() is not null;

        var (ready, reason) = EvaluateReadiness(options.Enabled, deliveryConfigured, state.Status);

        return new PaymentRequestEventOutboxOperationalSnapshot(
            state.Status,
            ready,
            reason,
            state.LastCycleStartedAtUtc,
            state.LastCycleCompletedAtUtc,
            state.LastDeliveredCount,
            state.TotalDeliveredCount,
            state.TotalCycleCount,
            state.TotalFailedCycleCount,
            state.ConsecutiveFailures,
            state.LastError,
            store.PendingCount,
            store.ProcessingCount,
            store.RetryCount,
            store.DeliveredCount,
            store.DeadLetterCount,
            store.OldestUndeliveredAtUtc,
            store.LatestDeadLetterAtUtc);
    }

    private static (bool Ready, string Reason) EvaluateReadiness(
        bool enabled,
        bool deliveryConfigured,
        PaymentRequestEventOutboxDispatcherStatus status)
    {
        if (!enabled) return (false, "Dispatcher is disabled by configuration.");
        if (!deliveryConfigured) return (false, "Event delivery port is not configured.");
        return status switch
        {
            PaymentRequestEventOutboxDispatcherStatus.Idle => (true, "Dispatcher is idle and ready."),
            PaymentRequestEventOutboxDispatcherStatus.Running => (true, "Dispatcher is running."),
            PaymentRequestEventOutboxDispatcherStatus.Blocked => (false, "Dispatcher is blocked."),
            PaymentRequestEventOutboxDispatcherStatus.Faulted => (false, "Dispatcher is faulted."),
            PaymentRequestEventOutboxDispatcherStatus.Disabled => (false, "Dispatcher is disabled."),
            _ => (false, "Dispatcher state is unknown.")
        };
    }
}

public static class PaymentRequestEventOutboxOperationalEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestEventOutboxOperationalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/payment-request-event-outbox", GetHealthAsync);
        endpoints.MapGet("/health/ready/payment-request-event-outbox", GetReadinessAsync);
        return endpoints;
    }

    private static async Task<IResult> GetHealthAsync(
        PaymentRequestEventOutboxOperationalHealthService service,
        CancellationToken cancellationToken)
    {
        var snapshot = await service.GetSnapshotAsync(cancellationToken);
        return Results.Ok(snapshot);
    }

    private static async Task<IResult> GetReadinessAsync(
        PaymentRequestEventOutboxOperationalHealthService service,
        CancellationToken cancellationToken)
    {
        var snapshot = await service.GetSnapshotAsync(cancellationToken);
        return snapshot.Ready
            ? Results.Ok(snapshot)
            : Results.Json(snapshot, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
