namespace AfriWallet.Fraud.Decision.Domain.Rules;

public enum FraudRuleType
{
    DeviceRisk = 0,
    TransactionFraud = 1,
    CombinedRisk = 2,
    CriticalOverride = 3
}