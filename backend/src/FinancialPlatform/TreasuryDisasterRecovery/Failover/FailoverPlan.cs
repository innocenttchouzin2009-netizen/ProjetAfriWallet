namespace TreasuryDisasterRecovery.Failover;

public sealed class FailoverPlan
{
    public FailoverPlan(string primaryRegion, string secondaryRegion)
    {
        PrimaryRegion = primaryRegion;
        SecondaryRegion = secondaryRegion;
    }

    public string PrimaryRegion { get; }

    public string SecondaryRegion { get; }

    public FailoverStatus Status { get; private set; } = FailoverStatus.PrimaryActive;

    public DateTime? FailoverStartedAtUtc { get; private set; }

    public DateTime? FailoverCompletedAtUtc { get; private set; }

    public void BeginFailover()
    {
        if (Status != FailoverStatus.PrimaryActive)
            throw new InvalidOperationException("Failover cannot start from current state.");

        Status = FailoverStatus.FailoverInProgress;
        FailoverStartedAtUtc = DateTime.UtcNow;
    }

    public void CompleteFailover()
    {
        if (Status != FailoverStatus.FailoverInProgress)
            throw new InvalidOperationException("Failover is not in progress.");

        Status = FailoverStatus.SecondaryActive;
        FailoverCompletedAtUtc = DateTime.UtcNow;
    }
}

public enum FailoverStatus
{
    PrimaryActive,
    FailoverInProgress,
    SecondaryActive,
    FailbackInProgress
}