using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;
using AfriWallet.Compliance.TransactionMonitoring.Application.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Alerts;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Risk;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Rules;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Monitoring;

public sealed class TransactionMonitoringService
{
    private readonly ITransactionHistoryRepository _history;
    private readonly IMonitoringAlertRepository _alerts;
    private readonly IMonitoringRuleProvider _rules;
    private readonly IMonitoringAuditStore _audit;
    private readonly IMonitoringClock _clock;
    private readonly LargeAmountRuleEvaluator _largeAmount;
    private readonly VelocityRuleEvaluator _velocity;
    private readonly StructuringRuleEvaluator _structuring;
    private readonly GeographicRiskRuleEvaluator _geographicRisk;
    private readonly RepeatedBeneficiaryRuleEvaluator _repeatedBeneficiary;

    public TransactionMonitoringService(
        ITransactionHistoryRepository history,
        IMonitoringAlertRepository alerts,
        IMonitoringRuleProvider rules,
        IMonitoringAuditStore audit,
        IMonitoringClock clock,
        LargeAmountRuleEvaluator largeAmount,
        VelocityRuleEvaluator velocity,
        StructuringRuleEvaluator structuring,
        GeographicRiskRuleEvaluator geographicRisk,
        RepeatedBeneficiaryRuleEvaluator repeatedBeneficiary)
    {
        _history = history;
        _alerts = alerts;
        _rules = rules;
        _audit = audit;
        _clock = clock;
        _largeAmount = largeAmount;
        _velocity = velocity;
        _structuring = structuring;
        _geographicRisk = geographicRisk;
        _repeatedBeneficiary = repeatedBeneficiary;
    }

    public async Task<MonitoringResult> MonitorAsync(
        MonitorTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = command.Transaction.Normalize();
        var history = await _history.GetByAwidAsync(
            transaction.Awid,
            transaction.OccurredAtUtc.AddHours(-24),
            cancellationToken);
        var evaluations = _rules
            .GetRules()
            .Where(rule => rule.Enabled)
            .Select(rule => Evaluate(rule, transaction, history))
            .ToArray();
        var risk = TransactionRiskScore.FromPoints(
            evaluations.Where(evaluation => evaluation.Triggered).Sum(evaluation => evaluation.RiskPoints));
        MonitoringAlert? alert = null;

        if (risk.Score >= 30)
        {
            var severity = risk.Score switch
            {
                >= 80 => MonitoringSeverity.Critical,
                >= 60 => MonitoringSeverity.High,
                _ => MonitoringSeverity.Medium
            };
            alert = new MonitoringAlert(
                Guid.NewGuid(),
                transaction.TransactionId,
                transaction.Awid,
                severity,
                risk.Score,
                evaluations
                    .Where(evaluation => evaluation.Triggered)
                    .Select(evaluation => evaluation.RuleCode)
                    .ToArray(),
                _clock.UtcNow);
            await _alerts.AddAsync(alert, cancellationToken);
        }

        await _history.AddAsync(transaction, cancellationToken);
        await _audit.AppendAsync(
            new MonitoringAuditEvent(
                Guid.NewGuid(),
                transaction.TransactionId,
                transaction.Awid,
                "aml.transaction.monitored",
                command.Actor,
                _clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["riskScore"] = risk.Score.ToString(),
                    ["riskBand"] = risk.Band,
                    ["alertGenerated"] = (alert is not null).ToString()
                }),
            cancellationToken);

        return new MonitoringResult(transaction.TransactionId, risk, evaluations, alert);
    }

    private RuleEvaluation Evaluate(
        MonitoringRule rule,
        Domain.Transactions.MonitoredTransaction transaction,
        IReadOnlyCollection<Domain.Transactions.MonitoredTransaction> history) =>
        rule.Type switch
        {
            MonitoringRuleType.LargeAmount => _largeAmount.Evaluate(rule, transaction),
            MonitoringRuleType.HighVelocity => _velocity.Evaluate(rule, transaction, history),
            MonitoringRuleType.Structuring => _structuring.Evaluate(rule, transaction, history),
            MonitoringRuleType.GeographicRisk => _geographicRisk.Evaluate(rule, transaction),
            MonitoringRuleType.RepeatedBeneficiary => _repeatedBeneficiary.Evaluate(rule, transaction, history),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Type, "Unknown rule type.")
        };
}