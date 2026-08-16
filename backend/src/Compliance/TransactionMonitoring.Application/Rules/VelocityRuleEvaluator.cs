using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Rules;

public sealed class VelocityRuleEvaluator
{
    public RuleEvaluation Evaluate(
        MonitoringRule rule,
        MonitoredTransaction transaction,
        IReadOnlyCollection<MonitoredTransaction> history)
    {
        var since = transaction.OccurredAtUtc.AddMinutes(-10);
        var recent = history.Count(item =>
            item.OccurredAtUtc >= since &&
            item.OccurredAtUtc <= transaction.OccurredAtUtc);
        var triggered = recent >= 5;

        return new RuleEvaluation(
            rule.Code,
            rule.Type,
            triggered,
            triggered ? rule.RiskPoints : 0,
            triggered
                ? $"{recent} prior transactions within 10 minutes"
                : "velocity within expected range");
    }
}