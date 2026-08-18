namespace AfriWallet.Merchants.PaymentDecision.Domain.Decisions;
public enum PaymentAuthorizationDecisionType { Authorize = 0, Decline = 1, RequiresStepUp = 2, CaptureEligible = 3 }
