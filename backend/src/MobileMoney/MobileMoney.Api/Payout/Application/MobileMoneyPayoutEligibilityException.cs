namespace MobileMoney.Production.Payout.Application;

public sealed class MobileMoneyPayoutEligibilityException : InvalidOperationException
{
    public MobileMoneyPayoutEligibilityException(string code)
        : base($"Mobile Money payout is not eligible: {code}.")
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Eligibility error code is required.", nameof(code));

        Code = code.Trim();
    }

    public string Code { get; }
}
