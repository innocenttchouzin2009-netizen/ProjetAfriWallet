namespace AML.Contracts;

public sealed class MonitoringDecision
{
    public Guid CaseId { get; init; } = Guid.NewGuid();
    public MonitoringDecisionType Decision { get; init; }
    public string AlertLevel { get; init; } = "LOW";
    public int Score { get; init; }
    public IReadOnlyList<string> TriggeredRules { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AuditEvents { get; init; } = Array.Empty<string>();
    public MonitoringTelemetry? Telemetry { get; init; }
}

public sealed class MonitoringTelemetry
{
    public string Decision { get; init; } = string.Empty;
    public string AlertLevel { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TriggeredRuleCount { get; init; }
    public double EvaluationDurationMs { get; init; }
}
