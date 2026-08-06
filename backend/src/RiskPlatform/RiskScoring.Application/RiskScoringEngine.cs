using RiskScoring.Contracts;

namespace RiskScoring.Application;

public sealed class RiskScoringEngine
{
    private readonly RiskAggregationService _aggregationService = new();

    public RiskEvaluationResult Evaluate(RiskEvaluationRequest request)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var factors = _aggregationService.Aggregate(request);
        var auditEvents = new List<string> { "RISK_EVALUATION_STARTED" };

        var score = factors.Sum(x => x.Contribution);
        var decision = score >= 110 ? RiskDecisionType.Block : score >= 70 ? RiskDecisionType.ManualReview : score >= 20 ? RiskDecisionType.Challenge : RiskDecisionType.Allow;
        var riskBand = score >= 110 ? "CRITICAL" : score >= 70 ? "HIGH" : score >= 50 ? "MEDIUM" : score >= 25 ? "LOW" : "VERY_LOW";

        auditEvents.Add(decision switch
        {
            RiskDecisionType.Block => "RISK_BLOCKED",
            RiskDecisionType.ManualReview => "RISK_MANUAL_REVIEW",
            RiskDecisionType.Challenge => "RISK_CHALLENGED",
            _ => "RISK_ALLOWED"
        });

        var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
        var telemetry = new RiskTelemetry
        {
            Decision = decision.ToString().ToUpperInvariant(),
            RiskBand = riskBand,
            Score = score,
            TriggeredRuleCount = factors.Count,
            EvaluationDurationMs = durationMs
        };

        return new RiskEvaluationResult
        {
            Decision = decision,
            RiskBand = riskBand,
            Score = score,
            TriggeredRules = factors.Select(x => x.FactorId).ToList(),
            Factors = factors,
            AuditEvents = auditEvents,
            Telemetry = telemetry
        };
    }
}
