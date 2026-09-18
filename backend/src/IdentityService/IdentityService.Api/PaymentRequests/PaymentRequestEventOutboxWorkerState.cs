namespace IdentityService.Api.PaymentRequests;

public enum PaymentRequestEventOutboxWorkerStatus
{
    Idle = 1,
    Running = 2,
    Blocked = 3,
    Faulted = 4,
    Disabled = 5
}

public sealed record PaymentRequestEventOutboxWorkerSnapshot(
    PaymentRequestEventOutboxWorkerStatus Status,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    DateTimeOffset? LastSuccessfulCycleCompletedAtUtc,
    int LastDeliveredCount,
    long TotalDeliveredCount,
    long TotalCycleCount,
    long TotalFailedCycleCount,
    int ConsecutiveFailures,
    string? LastError);

public sealed class PaymentRequestEventOutboxWorkerState
{
    private readonly object gate = new();
    private PaymentRequestEventOutboxWorkerSnapshot snapshot = new(
        PaymentRequestEventOutboxWorkerStatus.Idle,
        null,
        null,
        null,
        0,
        0,
        0,
        0,
        0,
        null);

    public PaymentRequestEventOutboxWorkerSnapshot Snapshot
    {
        get
        {
            lock (gate) return snapshot;
        }
    }

    public void MarkDisabled() => Update(current => current with
    {
        Status = PaymentRequestEventOutboxWorkerStatus.Disabled,
        LastDeliveredCount = 0,
        LastError = null
    });

    public void MarkTransportUnavailable(string error) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxWorkerStatus.Blocked,
        LastDeliveredCount = 0,
        LastError = error
    });

    public void MarkCycleStarted(DateTimeOffset atUtc) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxWorkerStatus.Running,
        LastCycleStartedAtUtc = atUtc,
        LastError = null
    });

    public void MarkCycleSucceeded(DateTimeOffset atUtc, int deliveredCount) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxWorkerStatus.Idle,
        LastCycleCompletedAtUtc = atUtc,
        LastSuccessfulCycleCompletedAtUtc = atUtc,
        LastDeliveredCount = deliveredCount,
        TotalDeliveredCount = current.TotalDeliveredCount + deliveredCount,
        TotalCycleCount = current.TotalCycleCount + 1,
        ConsecutiveFailures = 0,
        LastError = null
    });

    public void MarkCycleFailed(DateTimeOffset atUtc, string error) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxWorkerStatus.Faulted,
        LastCycleCompletedAtUtc = atUtc,
        LastDeliveredCount = 0,
        TotalCycleCount = current.TotalCycleCount + 1,
        TotalFailedCycleCount = current.TotalFailedCycleCount + 1,
        ConsecutiveFailures = current.ConsecutiveFailures + 1,
        LastError = error
    });

    private void Update(Func<PaymentRequestEventOutboxWorkerSnapshot, PaymentRequestEventOutboxWorkerSnapshot> updater)
    {
        lock (gate) snapshot = updater(snapshot);
    }
}
