namespace Operations.Domain;

public sealed class Alert
{
    public Guid AlertId { get; init; } = Guid.NewGuid();

    public string Metric { get; init; } = string.Empty;

    public decimal Threshold { get; init; }

    public decimal CurrentValue { get; private set; }

    public AlertSeverity Severity { get; init; }

    public bool Acknowledged { get; private set; }

    public bool Escalated { get; private set; }

    public DateTimeOffset RaisedUtc { get; init; } = DateTimeOffset.UtcNow;

    public void UpdateCurrentValue(decimal value)
    {
        CurrentValue = value;
    }

    public void Acknowledge()
    {
        Acknowledged = true;
    }

    public void Escalate()
    {
        Escalated = true;
    }
}