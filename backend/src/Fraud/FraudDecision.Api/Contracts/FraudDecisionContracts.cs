namespace AfriWallet.Fraud.Decision.Api.Contracts;

public sealed record EvaluateFraudDecisionRequest(Guid TransactionId, string Awid, string DeviceId);