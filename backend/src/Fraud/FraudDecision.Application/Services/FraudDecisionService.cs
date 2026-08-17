using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Application.Policies;
using AfriWallet.Fraud.Decision.Domain.Decisions;
using AfriWallet.Fraud.Decision.Domain.Rules;

namespace AfriWallet.Fraud.Decision.Application.Services;

public sealed class FraudDecisionService(
    IDeviceRiskDecisionReader deviceRisk,
    ITransactionFraudDecisionReader transactionFraud,
    IFraudDecisionRepository repository,
    IFraudDecisionAuditStore audit,
    IFraudDecisionClock clock,
    FraudDecisionPolicy policy)
{
    public async Task<FraudDecisionResult> EvaluateAsync(
        EvaluateFraudDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TransactionId == Guid.Empty)
            throw new ArgumentException("Transaction id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Awid))
            throw new ArgumentException("AWID is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.DeviceId))
            throw new ArgumentException("Device id is required.", nameof(command));

        var transactionInput = await transactionFraud.GetByTransactionAsync(command.TransactionId, cancellationToken)
            ?? throw new InvalidOperationException("Transaction fraud input was not found.");
        if (!string.Equals(transactionInput.Awid.Trim(), command.Awid.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Transaction fraud AWID does not match the command AWID.");

        var deviceInput = await deviceRisk.GetLatestAsync(command.Awid.Trim(), command.DeviceId.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Device risk input was not found.");
        if (!string.Equals(deviceInput.Awid.Trim(), command.Awid.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Device risk AWID does not match the command AWID.");

        var transactionScore = Math.Clamp(transactionInput.Score, 0, 100);
        var deviceScore = Math.Clamp(deviceInput.Score, 0, 100);
        var weightedScore = Math.Clamp((int)Math.Round(transactionScore * 0.65 + deviceScore * 0.35, MidpointRounding.AwayFromZero), 0, 100);
        var criticalOverride = transactionScore >= 90 && deviceScore >= 80;
        var finalScore = criticalOverride ? 100 : weightedScore;

        var evaluations = new[]
        {
            new FraudRuleEvaluation("TX-FRAUD-SCORE", FraudRuleType.TransactionFraud, transactionScore > 0, transactionScore, $"transaction fraud score normalized to {transactionScore}"),
            new FraudRuleEvaluation("DEVICE-RISK-SCORE", FraudRuleType.DeviceRisk, deviceScore > 0, deviceScore, $"device risk score normalized to {deviceScore}"),
            new FraudRuleEvaluation("COMBINED-WEIGHTED-RISK", FraudRuleType.CombinedRisk, weightedScore >= 30, weightedScore, "transaction fraud contributes 65% and device risk contributes 35%"),
            new FraudRuleEvaluation("CRITICAL-OVERRIDE", FraudRuleType.CriticalOverride, criticalOverride, criticalOverride ? 100 : weightedScore, criticalOverride ? "critical transaction and device thresholds reached" : "critical override thresholds not reached")
        };

        var decision = new FraudDecision(
            Guid.NewGuid(),
            command.TransactionId,
            command.Awid,
            command.DeviceId,
            finalScore,
            policy.ResolveBand(finalScore),
            policy.ResolveAction(finalScore, criticalOverride),
            evaluations,
            clock.UtcNow);

        await repository.SaveAsync(decision, cancellationToken);
        await audit.AppendAsync(
            new FraudDecisionAuditEvent(
                Guid.NewGuid(),
                decision.DecisionId,
                decision.TransactionId,
                decision.Awid,
                decision.Action.ToString(),
                command.Actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["score"] = decision.Score.ToString(),
                    ["band"] = decision.Band.ToString(),
                    ["action"] = decision.Action.ToString(),
                    ["executionPerformed"] = "false"
                }),
            cancellationToken);

        return new FraudDecisionResult(
            decision.DecisionId,
            decision.TransactionId,
            decision.Awid,
            decision.DeviceId,
            decision.Score,
            decision.Band,
            decision.Action,
            decision.Evaluations,
            decision.DecidedAtUtc);
    }
}