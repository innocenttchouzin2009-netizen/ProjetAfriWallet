namespace MobileMoney.Production.Payout.Execution.Application;

public sealed class MobileMoneyPayoutExecutionPreparationException : Exception
{
    public MobileMoneyPayoutExecutionPreparationException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Execution error code is required.", nameof(code));

        Code = code;
    }

    public string Code { get; }
}
