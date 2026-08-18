using AfriWallet.Merchants.PaymentDecision.Domain.Decisions;
namespace AfriWallet.Merchants.PaymentDecision.Application.Results;
public sealed record MerchantPaymentDecisionResult(Guid DecisionId, Guid PaymentIntentId, Guid CheckoutSessionId, string MerchantId, PaymentAuthorizationDecisionType DecisionType, PaymentAuthorizationDecisionStatus Status, PaymentDecisionReasonCode ReasonCode, string PolicyVersion, int FactorCount, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
