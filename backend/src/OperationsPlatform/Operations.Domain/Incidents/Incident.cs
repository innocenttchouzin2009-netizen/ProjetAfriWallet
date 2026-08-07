namespace Operations.Domain;

public sealed class Incident
{
    public Guid IncidentId { get; init; } = Guid.NewGuid();

    public IncidentSeverity Severity { get; init; }

    public IncidentStatus Status { get; private set; } = IncidentStatus.Open;

    public string ServiceName { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTimeOffset OpenedUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? AcknowledgedUtc { get; private set; }

    public DateTimeOffset? ResolvedUtc { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public string Owner { get; private set; } = string.Empty;

    public List<string> Timeline { get; } = new();

    public Incident()
    {
        Timeline.Add($"{OpenedUtc:O} OPEN");
    }

    public void Acknowledge(string owner, string note)
    {
        if (Status != IncidentStatus.Open)
        {
            throw new InvalidOperationException("Only open incidents can be acknowledged.");
        }

        Status = IncidentStatus.Acknowledged;
        Owner = owner;
        AcknowledgedUtc = DateTimeOffset.UtcNow;
        Timeline.Add($"{AcknowledgedUtc:O} ACKNOWLEDGED {note}");
    }

    public void StartProgress(string owner, string note)
    {
        if (Status is not (IncidentStatus.Open or IncidentStatus.Acknowledged))
        {
            throw new InvalidOperationException("Only open or acknowledged incidents can move in progress.");
        }

        Status = IncidentStatus.InProgress;
        Owner = owner;
        Timeline.Add($"{DateTimeOffset.UtcNow:O} IN_PROGRESS {note}");
    }

    public void Resolve(string resolution)
    {
        if (Status is not (IncidentStatus.Acknowledged or IncidentStatus.InProgress))
        {
            throw new InvalidOperationException("Only acknowledged or in progress incidents can be resolved.");
        }

        Status = IncidentStatus.Resolved;
        ResolvedUtc = DateTimeOffset.UtcNow;
        Timeline.Add($"{ResolvedUtc:O} RESOLVED {resolution}");
    }

    public void Close(string note)
    {
        if (Status != IncidentStatus.Resolved)
        {
            throw new InvalidOperationException("Only resolved incidents can be closed.");
        }

        Status = IncidentStatus.Closed;
        ClosedUtc = DateTimeOffset.UtcNow;
        Timeline.Add($"{ClosedUtc:O} CLOSED {note}");
    }
}