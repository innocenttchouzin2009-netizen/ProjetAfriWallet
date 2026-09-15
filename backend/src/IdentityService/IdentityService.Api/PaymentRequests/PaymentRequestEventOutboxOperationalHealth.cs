using AfriWallet.PaymentRequests.Application;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestEventOutboxOperationalSnapshot(
    PaymentRequestEventOutboxWorkerStatus Status,
    bool Ready,
    string ReadinessReason,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    DateTimeOffset? LastSuccessfulCycleCompletedAtUtc,
    int LastDeliveredCount,
    long TotalDeliveredCount,
    long TotalCycleCount,
    long TotalFailedCycleCount,
    int ConsecutiveFailures,
    long PendingCount,
    long ProcessingCount,
    long RetryCount,
    long DeliveredCount,
    long DeadLetterCount,
    DateTimeOffset? OldestUndeliveredAtUtc,
    DateTimeOffset? LatestDeadLetterAtUtc);

public sealed class PaymentRequestEventOutboxOperationalHealthService(
    PaymentRequestEventOutboxWorkerState workerState,
    IPaymentRequestEventOutboxDiagnostics diagnostics,
    IServiceProvider serviceProvider)
{
    public async Task<PaymentRequestEventOutboxOperationalSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var worker = workerState.Snapshot;
        var backlog = await diagnostics.GetSnapshotAsync(cancellationToken);
        var transportConfigured = serviceProvider.GetService<IPaymentRequestEventTransport>() is not null;
        var (ready, reason) = EvaluateReadiness(transportConfigured, worker.Status);

        return new PaymentRequestEventOutboxOperationalSnapshot(
            worker.Status,
            ready,
            reason,
            worker.LastCycleStartedAtUtc,
            worker.LastCycleCompletedAtUtc,
            worker.LastSuccessfulCycleCompletedAtUtc,
            worker.LastDeliveredCount,
            worker.TotalDeliveredCount,
            worker.TotalCycleCount,
            worker.TotalFailedCycleCount,
            worker.ConsecutiveFailures,
            backlog.PendingCount,
            backlog.ProcessingCount,
            backlog.RetryCount,
            backlog.DeliveredCount,
            backlog.DeadLetterCount,
            backlog.OldestUndeliveredAtUtc,
            backlog.LatestDeadLetterAtUtc);
    }

    private static (bool Ready, string Reason) EvaluateReadiness(
        bool transportConfigured,
        PaymentRequestEventOutboxWorkerStatus status)
    {
        if (!transportConfigured)
        {
            return (false, "Event transport is not configured.");
        }

        return status switch
        {
            PaymentRequestEventOutboxWorkerStatus.Idle => (true, "Outbox worker is idle and ready."),
            PaymentRequestEventOutboxWorkerStatus.Running => (true, "Outbox worker is running."),
            PaymentRequestEventOutboxWorkerStatus.Blocked => (false, "Outbox worker is blocked."),
            PaymentRequestEventOutboxWorkerStatus.Faulted => (false, "Outbox worker is faulted."),
            _ => (false, "Outbox worker state is unknown.")
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
