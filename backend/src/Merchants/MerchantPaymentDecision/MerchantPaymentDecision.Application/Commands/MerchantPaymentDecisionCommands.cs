namespace AfriWallet.Merchants.PaymentDecision.Application.Commands;
public sealed record EvaluatePaymentAuthorizationCommand(Guid PaymentIntentId, string Actor);
public sealed record CompletePaymentStepUpCommand(Guid DecisionId, string Actor);
public sealed record MarkCaptureEligibleCommand(Guid DecisionId, string Actor);
public sealed record ReevaluatePaymentAuthorizationCommand(Guid PaymentIntentId, string Reason, string Actor);
