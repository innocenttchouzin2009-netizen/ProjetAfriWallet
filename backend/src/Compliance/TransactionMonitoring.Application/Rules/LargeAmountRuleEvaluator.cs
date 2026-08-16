using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Rules;

public sealed class LargeAmountRuleEvaluator
{
    public RuleEvaluation Evaluate(MonitoringRule rule, MonitoredTransaction transaction)
    {
        var threshold = transaction.CurrencyCode switch
        {
            "EUR" => 1_000_000L,
            "USD" => 1_000_000L,
            "XAF" => 5_000_000L,
            "XOF" => 5_000_000L,
            _ => 1_000_000L
        };
        var triggered = transaction.AmountMinor >= threshold;

        return new RuleEvaluation(
            rule.Code,
            rule.Type,
            triggered,
            triggered ? rule.RiskPoints : 0,
            triggered
                ? $"amount_minor >= {threshold}"
                : "amount below monitoring threshold");
    }
}