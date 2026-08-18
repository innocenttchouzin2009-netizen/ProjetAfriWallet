namespace AfriWallet.Merchants.PaymentDecision.Domain.Policies;
public sealed record PaymentDecisionPolicyVersion(string PolicyId, string Version)
{
    public static PaymentDecisionPolicyVersion Current => new("AFW-MERCHANT-PAYMENT-AUTHORIZATION", "1.0");
    public override string ToString() => $"{PolicyId}:{Version}";
}
