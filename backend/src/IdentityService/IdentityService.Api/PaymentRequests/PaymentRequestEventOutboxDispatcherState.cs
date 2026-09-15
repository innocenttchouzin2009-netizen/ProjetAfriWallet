namespace IdentityService.Api.PaymentRequests;

public enum PaymentRequestEventOutboxDispatcherStatus
{
    Disabled = 1,
    Idle = 2,
    Running = 3,
    Blocked = 4,
    Faulted = 5
}

public sealed record PaymentRequestEventOutboxDispatcherSnapshot(
    PaymentRequestEventOutboxDispatcherStatus Status,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    int LastDeliveredCount,
    long TotalDeliveredCount,
    int ConsecutiveFailures,
    string? LastError);

public sealed class PaymentRequestEventOutboxDispatcherState
{
    private readonly object gate = new();
    private PaymentRequestEventOutboxDispatcherSnapshot snapshot = new(
        PaymentRequestEventOutboxDispatcherStatus.Idle,
        null,
        null,
        0,
        0,
        0,
        null);

    public PaymentRequestEventOutboxDispatcherSnapshot Snapshot
    {
        get
        {
            lock (gate) return snapshot;
        }
    }

    public void MarkDisabled() => Update(current => current with
    {
        Status = PaymentRequestEventOutboxDispatcherStatus.Disabled,
        LastError = null
    });

    public void MarkIdle() => Update(current => current with
    {
        Status = PaymentRequestEventOutboxDispatcherStatus.Idle,
        LastError = null
    });

    public void MarkBlocked(DateTimeOffset atUtc, string error) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxDispatcherStatus.Blocked,
        LastCycleCompletedAtUtc = atUtc,
        LastDeliveredCount = 0,
        ConsecutiveFailures = current.ConsecutiveFailures + 1,
        LastError = error
    });

    public void MarkCycleStarted(DateTimeOffset atUtc) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxDispatcherStatus.Running,
        LastCycleStartedAtUtc = atUtc,
        LastError = null
    });

    public void MarkCycleCompleted(DateTimeOffset atUtc, int deliveredCount) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxDispatcherStatus.Idle,
        LastCycleCompletedAtUtc = atUtc,
        LastDeliveredCount = deliveredCount,
        TotalDeliveredCount = current.TotalDeliveredCount + deliveredCount,
        ConsecutiveFailures = 0,
        LastError = null
    });

    public void MarkCycleFailed(DateTimeOffset atUtc, string error) => Update(current => current with
    {
        Status = PaymentRequestEventOutboxDispatcherStatus.Faulted,
        LastCycleCompletedAtUtc = atUtc,
        LastDeliveredCount = 0,
        ConsecutiveFailures = current.ConsecutiveFailures + 1,
        LastError = error
    });

    private void Update(Func<PaymentRequestEventOutboxDispatcherSnapshot, PaymentRequestEventOutboxDispatcherSnapshot> updater)
    {
        lock (gate) snapshot = updater(snapshot);
    }
}
