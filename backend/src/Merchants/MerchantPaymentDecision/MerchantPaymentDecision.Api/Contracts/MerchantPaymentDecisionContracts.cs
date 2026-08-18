namespace AfriWallet.Merchants.PaymentDecision.Api.Contracts;
public sealed record EvaluatePaymentAuthorizationRequest(Guid PaymentIntentId);
public sealed record ReevaluatePaymentAuthorizationRequest(Guid PaymentIntentId,string Reason);
