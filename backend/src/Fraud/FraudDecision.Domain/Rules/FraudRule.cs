namespace AfriWallet.Fraud.Decision.Domain.Rules;

public sealed record FraudRule(
    string Code,
    FraudRuleType Type,
    string Description,
    bool Enabled,
    int Weight);