using AfriWallet.Merchants.PaymentDecision.Application.Abstractions;
using AfriWallet.Merchants.PaymentDecision.Domain.Decisions;
using AfriWallet.Merchants.PaymentDecision.Domain.Policies;
namespace AfriWallet.Merchants.PaymentDecision.Application.Policies;
public sealed record PaymentAuthorizationEvaluation(PaymentAuthorizationDecisionType DecisionType, PaymentDecisionReasonCode ReasonCode, IReadOnlyCollection<PaymentDecisionFactor> Factors, PaymentDecisionPolicyVersion PolicyVersion);
public sealed class MerchantPaymentAuthorizationPolicy
{
    public const long MaximumAmountMinor = 1_000_000; public const int StepUpThreshold = 60; public const int DeclineThreshold = 85;
    public PaymentAuthorizationEvaluation Evaluate(PaymentIntentDecisionSnapshot s, DateTimeOffset now)
    {
        PaymentAuthorizationDecisionType type; PaymentDecisionReasonCode code;
        if (!string.Equals(s.Status,"ReadyForAuthorization",StringComparison.OrdinalIgnoreCase)) { type=PaymentAuthorizationDecisionType.Decline; code=s.Status.Equals("Expired",StringComparison.OrdinalIgnoreCase)?PaymentDecisionReasonCode.PaymentIntentExpired:s.Status.Equals("Cancelled",StringComparison.OrdinalIgnoreCase)?PaymentDecisionReasonCode.PaymentIntentCancelled:PaymentDecisionReasonCode.PaymentIntentNotReady; }
        else if (now >= s.ExpiresAtUtc) { type=PaymentAuthorizationDecisionType.Decline; code=PaymentDecisionReasonCode.PaymentIntentExpired; }
        else if (!string.Equals(s.MerchantRegistryStatus,"Active",StringComparison.OrdinalIgnoreCase)||!string.Equals(s.MerchantVerificationStatus,"Verified",StringComparison.OrdinalIgnoreCase)) { type=PaymentAuthorizationDecisionType.Decline; code=PaymentDecisionReasonCode.MerchantNotEligible; }
        else if (!string.Equals(s.Currency,"XOF",StringComparison.OrdinalIgnoreCase)) { type=PaymentAuthorizationDecisionType.Decline; code=PaymentDecisionReasonCode.CurrencyMismatch; }
        else if (s.AmountMinor <= 0 || s.AmountMinor > MaximumAmountMinor) { type=PaymentAuthorizationDecisionType.Decline; code=PaymentDecisionReasonCode.AmountOutsidePolicy; }
        else if (string.IsNullOrWhiteSpace(s.PaymentMethodType)) { type=PaymentAuthorizationDecisionType.Decline; code=PaymentDecisionReasonCode.UnsupportedPaymentMethod; }
        else if (s.RiskScore >= DeclineThreshold) { type=PaymentAuthorizationDecisionType.Decline; code=PaymentDecisionReasonCode.RiskSignalCritical; }
        else if (s.RiskScore >= StepUpThreshold) { type=PaymentAuthorizationDecisionType.RequiresStepUp; code=PaymentDecisionReasonCode.StepUpRequired; }
        else { type=PaymentAuthorizationDecisionType.Authorize; code=PaymentDecisionReasonCode.LowRiskAuthorization; }
        return new(type,code,new[]{new PaymentDecisionFactor("PAYMENT_INTENT_STATUS",s.Status,"AFW-DLV-0019.3"),new PaymentDecisionFactor("AMOUNT_MINOR",s.AmountMinor.ToString(),"AFW-DLV-0019.3"),new PaymentDecisionFactor("CURRENCY",s.Currency,"AFW-DLV-0019.3"),new PaymentDecisionFactor("PAYMENT_METHOD_TYPE",string.IsNullOrWhiteSpace(s.PaymentMethodType)?"Not supplied":s.PaymentMethodType,"AFW-DLV-0019.3"),new PaymentDecisionFactor("RISK_SCORE",s.RiskScore.ToString(),"Fraud/Risk Snapshot")},PaymentDecisionPolicyVersion.Current);
    }
}
