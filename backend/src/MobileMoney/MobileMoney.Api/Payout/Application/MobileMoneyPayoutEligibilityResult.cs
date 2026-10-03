namespace MobileMoney.Production.Payout.Application;

public static class MobileMoneyPayoutEligibilityCodes
{
    public const string CorridorNotSupported = "PAYOUT_CORRIDOR_NOT_SUPPORTED";
    public const string CorridorDisabled = "PAYOUT_CORRIDOR_DISABLED";
    public const string OperatorNotActivated = "PAYOUT_OPERATOR_NOT_ACTIVATED";
    public const string OutboundPayoutCapabilityRequired = "PAYOUT_OUTBOUND_CAPABILITY_REQUIRED";
}

public sealed record MobileMoneyPayoutEligibilityResult(
    bool IsEligible,
    string? FailureCode)
{
    public static MobileMoneyPayoutEligibilityResult Eligible() => new(true, null);

    public static MobileMoneyPayoutEligibilityResult Ineligible(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("Failure code is required.", nameof(failureCode));

        return new MobileMoneyPayoutEligibilityResult(false, failureCode.Trim());
    }
}
