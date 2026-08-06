using AML.Contracts;

namespace AML.Application;

public sealed class MonitoringEngine
{
    private readonly RuleEngine _ruleEngine = new();

    public MonitoringDecision Evaluate(MonitoringEvaluationRequest request)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var triggeredRules = new List<string>();
        var auditEvents = new List<string> { "AML_EVALUATION_STARTED" };
        var score = 0;

        var ruleResults = _ruleEngine.Evaluate(request);
        foreach (var result in ruleResults)
        {
            triggeredRules.Add(result.RuleId);
            score += result.ScoreDelta;
            auditEvents.Add("AML_RULE_TRIGGERED:" + result.RuleId);
        }

        var decision = score >= 85 ? MonitoringDecisionType.Escalate : score >= 25 ? MonitoringDecisionType.Review : MonitoringDecisionType.Clear;
        var alertLevel = score >= 85 ? "CRITICAL" : score >= 60 ? "HIGH" : score >= 25 ? "MEDIUM" : "LOW";
        auditEvents.Add(decision switch
        {
            MonitoringDecisionType.Escalate => "AML_ESCALATED",
            MonitoringDecisionType.Review => "AML_REVIEW_REQUIRED",
            _ => "AML_CLEARED"
        });

        var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
        var telemetry = new MonitoringTelemetry
        {
            Decision = decision.ToString().ToUpperInvariant(),
            AlertLevel = alertLevel,
            Score = score,
            TriggeredRuleCount = triggeredRules.Count,
            EvaluationDurationMs = durationMs
        };

        return new MonitoringDecision
        {
            Decision = decision,
            AlertLevel = alertLevel,
            Score = score,
            TriggeredRules = triggeredRules,
            AuditEvents = auditEvents,
            Telemetry = telemetry
        };
    }
}
