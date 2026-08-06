namespace Fraud.Domain;

public sealed class FraudDecisionRecord
{
    public Guid CaseId { get; init; } = Guid.NewGuid();
    public string Decision { get; init; } = "APPROVE";
    public string RiskLevel { get; init; } = "LOW";
    public int Score { get; init; }
    public IReadOnlyList<string> TriggeredRules { get; init; } = Array.Empty<string>();
}
