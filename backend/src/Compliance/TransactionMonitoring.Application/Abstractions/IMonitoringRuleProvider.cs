using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

public interface IMonitoringRuleProvider
{
    IReadOnlyCollection<MonitoringRule> GetRules();
}