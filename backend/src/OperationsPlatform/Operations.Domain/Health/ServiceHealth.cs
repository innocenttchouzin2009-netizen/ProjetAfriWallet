namespace Operations.Domain;

public sealed class ServiceHealth
{
    public Guid ServiceId { get; init; } = Guid.NewGuid();

    public string ServiceName { get; init; } = string.Empty;

    public HealthStatus Status { get; private set; }

    public double AvailabilityPercent { get; private set; }

    public TimeSpan ResponseTime { get; private set; }

    public DateTimeOffset LastHeartbeatUtc { get; private set; }

    public void ReportHealthy(TimeSpan responseTime)
    {
        Status = HealthStatus.Healthy;
        AvailabilityPercent = 99.99;
        ResponseTime = responseTime;
        LastHeartbeatUtc = DateTimeOffset.UtcNow;
    }

    public void ReportDegraded(TimeSpan responseTime)
    {
        Status = HealthStatus.Degraded;
        AvailabilityPercent = 97.5;
        ResponseTime = responseTime;
        LastHeartbeatUtc = DateTimeOffset.UtcNow;
    }

    public void ReportMaintenance()
    {
        Status = HealthStatus.Maintenance;
        AvailabilityPercent = 100;
        LastHeartbeatUtc = DateTimeOffset.UtcNow;
    }

    public void ReportFailure()
    {
        Status = HealthStatus.Unhealthy;
        AvailabilityPercent = 0;
        LastHeartbeatUtc = DateTimeOffset.UtcNow;
    }

    public void SetAvailabilityPercent(double availabilityPercent)
    {
        AvailabilityPercent = availabilityPercent;
    }
}