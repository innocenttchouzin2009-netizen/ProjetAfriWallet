using AML.Contracts;

namespace AML.Application;

public sealed class RuleEngine
{
    private readonly IReadOnlyList<MonitoringRule> _rules = new List<MonitoringRule>
    {
        new("daily-threshold", 25, request => request.DailyAmount >= 1500000),
        new("monthly-threshold", 30, request => request.MonthlyAmount >= 5000000),
        new("structuring", 35, request => request.TransactionFrequency >= 5 && request.Amount <= 50000m),
        new("new-account-activity", 25, request => request.NewAccount && request.TransactionFrequency >= 3),
        new("beneficiary-concentration", 30, request => request.BeneficiaryCount >= 5),
        new("high-velocity", 30, request => request.TransactionFrequency >= 8),
        new("multi-currency", 15, request => request.MultiCurrency),
        new("multi-channel", 15, request => request.MultiChannel)
    };

    public IReadOnlyList<MonitoringRuleEvaluationResult> Evaluate(MonitoringEvaluationRequest request)
    {
        var results = new List<MonitoringRuleEvaluationResult>();
        foreach (var rule in _rules)
        {
            if (rule.Predicate(request))
            {
                results.Add(new MonitoringRuleEvaluationResult(rule.RuleId, rule.ScoreDelta));
            }
        }

        return results;
    }
}

public sealed record MonitoringRule(string RuleId, int ScoreDelta, Func<MonitoringEvaluationRequest, bool> Predicate);
public sealed record MonitoringRuleEvaluationResult(string RuleId, int ScoreDelta);
