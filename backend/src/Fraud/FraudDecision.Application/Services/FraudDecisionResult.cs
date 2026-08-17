using AfriWallet.Fraud.Decision.Domain.Decisions;
using AfriWallet.Fraud.Decision.Domain.Rules;

namespace AfriWallet.Fraud.Decision.Application.Services;

public sealed record FraudDecisionResult(
    Guid DecisionId,
    Guid TransactionId,
    string Awid,
    string DeviceId,
    int Score,
    FraudDecisionBand Band,
    FraudDecisionAction Action,
    IReadOnlyCollection<FraudRuleEvaluation> Evaluations,
    DateTimeOffset DecidedAtUtc);