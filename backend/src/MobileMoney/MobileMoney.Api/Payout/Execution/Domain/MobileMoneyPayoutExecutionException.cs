namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed class MobileMoneyPayoutExecutionException : InvalidOperationException
{
    public MobileMoneyPayoutExecutionException(string code)
        : base($"Mobile Money payout execution rejected: {code}.")
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Execution error code is required.", nameof(code));

        Code = code.Trim();
    }

    public string Code { get; }
}
