using AfriWallet.Merchants.PaymentDecision.Domain.Policies;

namespace AfriWallet.Merchants.PaymentDecision.Domain.Decisions;

public sealed class MerchantPaymentDecision
{
    private readonly List<PaymentDecisionFactor> _factors = new();
    public MerchantPaymentDecision(Guid decisionId, Guid paymentIntentId, Guid checkoutSessionId, string merchantId, PaymentAuthorizationDecisionType decisionType, PaymentDecisionReasonCode reasonCode, PaymentDecisionPolicyVersion policyVersion, IEnumerable<PaymentDecisionFactor> factors, DateTimeOffset createdAtUtc)
    {
        if (decisionId == Guid.Empty || paymentIntentId == Guid.Empty || checkoutSessionId == Guid.Empty) throw new ArgumentException("Decision, payment intent and checkout session ids are required.");
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        DecisionId = decisionId; PaymentIntentId = paymentIntentId; CheckoutSessionId = checkoutSessionId; MerchantId = merchantId.Trim(); DecisionType = decisionType; ReasonCode = reasonCode; PolicyVersion = policyVersion ?? throw new ArgumentNullException(nameof(policyVersion)); _factors.AddRange(factors ?? throw new ArgumentNullException(nameof(factors))); CreatedAtUtc = createdAtUtc; UpdatedAtUtc = createdAtUtc;
        Status = decisionType switch { PaymentAuthorizationDecisionType.Decline => PaymentAuthorizationDecisionStatus.Declined, PaymentAuthorizationDecisionType.RequiresStepUp => PaymentAuthorizationDecisionStatus.PendingStepUp, _ => PaymentAuthorizationDecisionStatus.Approved };
    }
    public Guid DecisionId { get; }
    public Guid PaymentIntentId { get; }
    public Guid CheckoutSessionId { get; }
    public string MerchantId { get; }
    public PaymentAuthorizationDecisionType DecisionType { get; private set; }
    public PaymentAuthorizationDecisionStatus Status { get; private set; }
    public PaymentDecisionReasonCode ReasonCode { get; private set; }
    public PaymentDecisionPolicyVersion PolicyVersion { get; }
    public IReadOnlyCollection<PaymentDecisionFactor> Factors => _factors.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void CompleteStepUp(DateTimeOffset now) { if (Status != PaymentAuthorizationDecisionStatus.PendingStepUp) throw new InvalidOperationException("Decision is not waiting for step-up."); DecisionType = PaymentAuthorizationDecisionType.Authorize; Status = PaymentAuthorizationDecisionStatus.Approved; ReasonCode = PaymentDecisionReasonCode.StepUpSatisfied; UpdatedAtUtc = now; }
    public void MarkCaptureEligible(DateTimeOffset now) { if (Status != PaymentAuthorizationDecisionStatus.Approved) throw new InvalidOperationException("Only approved authorization may become capture eligible."); if (DecisionType != PaymentAuthorizationDecisionType.Authorize) throw new InvalidOperationException("Authorization decision required before capture eligibility."); DecisionType = PaymentAuthorizationDecisionType.CaptureEligible; ReasonCode = PaymentDecisionReasonCode.CaptureEligibilityConfirmed; UpdatedAtUtc = now; }
    public void Supersede(DateTimeOffset now) { if (Status == PaymentAuthorizationDecisionStatus.Superseded) throw new InvalidOperationException("Decision already superseded."); Status = PaymentAuthorizationDecisionStatus.Superseded; UpdatedAtUtc = now; }
}
