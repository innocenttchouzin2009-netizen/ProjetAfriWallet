using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Rules;

public sealed class RepeatedBeneficiaryRuleEvaluator
{
    public RuleEvaluation Evaluate(
        MonitoringRule rule,
        MonitoredTransaction transaction,
        IReadOnlyCollection<MonitoredTransaction> history)
    {
        if (string.IsNullOrWhiteSpace(transaction.BeneficiaryId))
        {
            return new RuleEvaluation(rule.Code, rule.Type, false, 0, "no beneficiary");
        }

        var since = transaction.OccurredAtUtc.AddHours(-1);
        var count = history.Count(item =>
            item.OccurredAtUtc >= since &&
            item.OccurredAtUtc <= transaction.OccurredAtUtc &&
            string.Equals(
                item.BeneficiaryId,
                transaction.BeneficiaryId,
                StringComparison.OrdinalIgnoreCase));
        var triggered = count >= 4;

        return new RuleEvaluation(
            rule.Code,
            rule.Type,
            triggered,
            triggered ? rule.RiskPoints : 0,
            triggered
                ? $"{count} prior transfers to same beneficiary within one hour"
                : "beneficiary frequency within expected range");
    }
}