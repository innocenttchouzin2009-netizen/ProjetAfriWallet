namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

public sealed record MonitoringRule(
    string Code,
    MonitoringRuleType Type,
    string Description,
    bool Enabled,
    int RiskPoints);