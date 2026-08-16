using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Rules;

public sealed class GeographicRiskRuleEvaluator
{
    private static readonly HashSet<string> SandboxHighRiskCountries =
        new(["XZ", "XY"], StringComparer.OrdinalIgnoreCase);

    public RuleEvaluation Evaluate(MonitoringRule rule, MonitoredTransaction transaction)
    {
        var triggered = SandboxHighRiskCountries.Contains(transaction.CountryCode);

        return new RuleEvaluation(
            rule.Code,
            rule.Type,
            triggered,
            triggered ? rule.RiskPoints : 0,
            triggered
                ? "sandbox high-risk geography signal"
                : "geography not flagged");
    }
}