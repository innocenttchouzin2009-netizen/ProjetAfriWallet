namespace RiskScoring.Contracts;

public sealed class RiskEvaluationResult
{
    public Guid EvaluationId { get; init; } = Guid.NewGuid();
    public RiskDecisionType Decision { get; init; }
    public string RiskBand { get; init; } = "VERY_LOW";
    public int Score { get; init; }
    public IReadOnlyList<string> TriggeredRules { get; init; } = Array.Empty<string>();
    public IReadOnlyList<RiskFactorContribution> Factors { get; init; } = Array.Empty<RiskFactorContribution>();
    public IReadOnlyList<string> AuditEvents { get; init; } = Array.Empty<string>();
    public RiskTelemetry? Telemetry { get; init; }
}

public sealed class RiskFactorContribution
{
    public string FactorId { get; init; } = string.Empty;
    public int Weight { get; init; }
    public int Contribution { get; init; }
}

public sealed class RiskTelemetry
{
    public string Decision { get; init; } = string.Empty;
    public string RiskBand { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TriggeredRuleCount { get; init; }
    public double EvaluationDurationMs { get; init; }
}
