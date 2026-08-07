namespace Operations.Domain;

public sealed class DeploymentRecord
{
    public Guid DeploymentId { get; init; } = Guid.NewGuid();

    public string ServiceName { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;

    public DeploymentStatus Status { get; private set; } = DeploymentStatus.Pending;

    public DateTimeOffset DeployedUtc { get; init; } = DateTimeOffset.UtcNow;

    public string DeployedBy { get; init; } = string.Empty;

    public List<string> Timeline { get; } = new();

    public DeploymentRecord()
    {
        Timeline.Add($"{DeployedUtc:O} PENDING");
    }

    public void MarkRunning(string note)
    {
        Status = DeploymentStatus.Running;
        Timeline.Add($"{DateTimeOffset.UtcNow:O} RUNNING {note}");
    }

    public void MarkSucceeded(string note)
    {
        Status = DeploymentStatus.Succeeded;
        Timeline.Add($"{DateTimeOffset.UtcNow:O} SUCCEEDED {note}");
    }

    public void MarkFailed(string note)
    {
        Status = DeploymentStatus.Failed;
        Timeline.Add($"{DateTimeOffset.UtcNow:O} FAILED {note}");
    }
}