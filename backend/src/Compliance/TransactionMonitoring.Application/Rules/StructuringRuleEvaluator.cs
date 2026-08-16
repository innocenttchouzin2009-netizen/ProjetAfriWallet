using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Rules;

public sealed class StructuringRuleEvaluator
{
    public RuleEvaluation Evaluate(
        MonitoringRule rule,
        MonitoredTransaction transaction,
        IReadOnlyCollection<MonitoredTransaction> history)
    {
        var since = transaction.OccurredAtUtc.AddHours(-24);
        var comparable = history
            .Where(item =>
                item.OccurredAtUtc >= since &&
                item.OccurredAtUtc <= transaction.OccurredAtUtc &&
                item.Direction == transaction.Direction &&
                item.CurrencyCode == transaction.CurrencyCode)
            .Append(transaction)
            .ToArray();
        var suspicious = comparable.Length >= 3 && comparable.All(item =>
            item.AmountMinor >= 700_000 &&
            item.AmountMinor < 1_000_000);

        return new RuleEvaluation(
            rule.Code,
            rule.Type,
            suspicious,
            suspicious ? rule.RiskPoints : 0,
            suspicious
                ? "multiple transactions clustered below large-amount threshold"
                : "no structuring pattern detected");
    }
}