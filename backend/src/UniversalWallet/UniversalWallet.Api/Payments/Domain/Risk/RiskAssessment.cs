namespace UniversalWallet.Api.Payments.Domain.Risk;

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RecommendedRiskAction
{
    Allow,
    StepUp,
    Review,
    Block
}

public sealed class RiskAssessment
{
    public int Score { get; init; }
    public RiskLevel Level { get; init; }
    public RecommendedRiskAction RecommendedAction { get; init; }
    public string RulesVersion { get; init; } = string.Empty;
}
