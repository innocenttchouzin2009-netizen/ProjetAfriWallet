namespace AfriWallet.Fraud.Decision.Application.Services;

public sealed record EvaluateFraudDecisionCommand(
    Guid TransactionId,
    string Awid,
    string DeviceId,
    string Actor);