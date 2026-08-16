using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

namespace AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

public sealed class SandboxMonitoringRuleProvider : IMonitoringRuleProvider
{
    private static readonly MonitoringRule[] Rules =
    [
        new("AML-LARGE-AMOUNT", MonitoringRuleType.LargeAmount, "Large amount monitoring signal", true, 40),
        new("AML-HIGH-VELOCITY", MonitoringRuleType.HighVelocity, "High transaction velocity", true, 35),
        new("AML-STRUCTURING", MonitoringRuleType.Structuring, "Possible transaction structuring", true, 55),
        new("AML-GEO-RISK", MonitoringRuleType.GeographicRisk, "Sandbox geographic risk signal", true, 30),
        new("AML-REPEATED-BENEFICIARY", MonitoringRuleType.RepeatedBeneficiary, "Repeated beneficiary frequency", true, 25)
    ];

    public IReadOnlyCollection<MonitoringRule> GetRules() => Rules;
}