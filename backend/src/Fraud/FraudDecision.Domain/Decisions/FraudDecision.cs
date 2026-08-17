using AfriWallet.Fraud.Decision.Domain.Rules;

namespace AfriWallet.Fraud.Decision.Domain.Decisions;

public sealed class FraudDecision
{
    public FraudDecision(
        Guid decisionId,
        Guid transactionId,
        string awid,
        string deviceId,
        int score,
        FraudDecisionBand band,
        FraudDecisionAction action,
        IReadOnlyCollection<FraudRuleEvaluation> evaluations,
        DateTimeOffset decidedAtUtc)
    {
        if (decisionId == Guid.Empty)
            throw new ArgumentException("Decision id is required.");
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction id is required.");
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.");
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device id is required.");

        DecisionId = decisionId;
        TransactionId = transactionId;
        Awid = awid.Trim();
        DeviceId = deviceId.Trim();
        Score = Math.Clamp(score, 0, 100);
        Band = band;
        Action = action;
        Evaluations = evaluations ?? throw new ArgumentNullException(nameof(evaluations));
        DecidedAtUtc = decidedAtUtc;
    }

    public Guid DecisionId { get; }
    public Guid TransactionId { get; }
    public string Awid { get; }
    public string DeviceId { get; }
    public int Score { get; }
    public FraudDecisionBand Band { get; }
    public FraudDecisionAction Action { get; }
    public IReadOnlyCollection<FraudRuleEvaluation> Evaluations { get; }
    public DateTimeOffset DecidedAtUtc { get; }
}