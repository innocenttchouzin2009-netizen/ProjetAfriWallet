namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

public enum MonitoringRuleType
{
    LargeAmount = 0,
    HighVelocity = 1,
    Structuring = 2,
    GeographicRisk = 3,
    RepeatedBeneficiary = 4
}