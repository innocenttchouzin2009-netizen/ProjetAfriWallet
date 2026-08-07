namespace Operations.Domain;

public sealed class DisasterRecoveryPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();

    public string Region { get; init; } = string.Empty;

    public TimeSpan RecoveryTimeObjective { get; init; }

    public TimeSpan RecoveryPointObjective { get; init; }

    public DateTimeOffset? LastTestUtc { get; private set; }

    public string Owner { get; init; } = string.Empty;

    public void RecordTest(DateTimeOffset utcNow)
    {
        LastTestUtc = utcNow;
    }
}