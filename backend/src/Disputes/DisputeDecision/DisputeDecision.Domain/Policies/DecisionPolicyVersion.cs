namespace AfriWallet.Disputes.Decision.Domain.Policies;

public sealed record DecisionPolicyVersion(string PolicyId, string Version)
{
    public static DecisionPolicyVersion Current => new("AFW-DISPUTE-RESOLUTION", "1.0");

    public override string ToString() => $"{PolicyId}:{Version}";
}
