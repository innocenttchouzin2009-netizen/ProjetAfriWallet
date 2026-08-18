namespace AfriWallet.Merchants.PaymentDecision.Domain.Decisions;
public enum PaymentAuthorizationDecisionStatus { Proposed = 0, PendingStepUp = 1, Approved = 2, Declined = 3, Superseded = 4 }
