using Fraud.Contracts;

namespace Fraud.Application;

public sealed class FraudEngine
{
    private readonly FraudRuleEngine _ruleEngine = new();

    public FraudDecision Evaluate(FraudEvaluationRequest request)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var triggeredRules = new List<string>();
        var auditEvents = new List<string> { "FRAUD_EVALUATION_STARTED" };

        var score = 0;
        var ruleResults = _ruleEngine.Evaluate(request);
        foreach (var result in ruleResults)
        {
            triggeredRules.Add(result.RuleId);
            score += result.ScoreDelta;
            auditEvents.Add("FRAUD_RULE_TRIGGERED:" + result.RuleId);
        }

        var configuration = new FraudConfiguration();
        var decision = score >= configuration.BlockScoreThreshold ? FraudDecisionType.Block : score >= configuration.ReviewScoreThreshold ? FraudDecisionType.Review : FraudDecisionType.Approve;
        var riskLevel = score >= configuration.BlockScoreThreshold ? "CRITICAL" : score >= configuration.ReviewScoreThreshold ? "HIGH" : score >= 30 ? "MEDIUM" : "LOW";
        auditEvents.Add(decision switch
        {
            FraudDecisionType.Block => "FRAUD_BLOCKED",
            FraudDecisionType.Review => "FRAUD_REVIEW_REQUIRED",
            _ => "FRAUD_APPROVED"
        });

        var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
        var telemetry = new FraudTelemetry
        {
            Decision = decision.ToString().ToUpperInvariant(),
            RiskLevel = riskLevel,
            Score = score,
            TriggeredRuleCount = triggeredRules.Count,
            EvaluationDurationMs = durationMs
        };

        return new FraudDecision
        {
            Decision = decision,
            RiskLevel = riskLevel,
            Score = score,
            TriggeredRules = triggeredRules,
            AuditEvents = auditEvents,
            Telemetry = telemetry
        };
    }
}
