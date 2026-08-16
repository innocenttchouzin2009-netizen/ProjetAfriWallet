using AfriWallet.Compliance.TransactionMonitoring.Domain.Alerts;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Risk;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Monitoring;

public sealed record MonitoringResult(
    Guid TransactionId,
    TransactionRiskScore Risk,
    IReadOnlyCollection<RuleEvaluation> Evaluations,
    MonitoringAlert? Alert);