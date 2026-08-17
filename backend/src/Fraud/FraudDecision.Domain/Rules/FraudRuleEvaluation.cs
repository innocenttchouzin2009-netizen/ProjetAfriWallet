namespace AfriWallet.Fraud.Decision.Domain.Rules;

public sealed record FraudRuleEvaluation(
    string RuleCode,
    FraudRuleType RuleType,
    bool Triggered,
    int Score,
    string Reason);