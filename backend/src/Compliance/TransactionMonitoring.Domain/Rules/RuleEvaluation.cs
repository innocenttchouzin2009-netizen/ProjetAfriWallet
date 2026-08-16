namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

public sealed record RuleEvaluation(
    string RuleCode,
    MonitoringRuleType RuleType,
    bool Triggered,
    int RiskPoints,
    string Reason);