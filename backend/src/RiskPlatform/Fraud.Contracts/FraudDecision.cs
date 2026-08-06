namespace Fraud.Contracts;

public sealed class FraudDecision
{
    public Guid CaseId { get; init; } = Guid.NewGuid();
    public FraudDecisionType Decision { get; init; }
    public string RiskLevel { get; init; } = "LOW";
    public int Score { get; init; }
    public IReadOnlyList<string> TriggeredRules { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AuditEvents { get; init; } = Array.Empty<string>();
    public FraudTelemetry? Telemetry { get; init; }
}

public sealed class FraudTelemetry
{
    public string Decision { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TriggeredRuleCount { get; init; }
    public double EvaluationDurationMs { get; init; }
}
