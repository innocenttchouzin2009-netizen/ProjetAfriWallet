namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionIdempotencyKey
{
    private MobileMoneyPayoutExecutionIdempotencyKey(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MobileMoneyPayoutExecutionIdempotencyKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Idempotency key is required.", nameof(value));

        return new MobileMoneyPayoutExecutionIdempotencyKey(value.Trim());
    }

    public override string ToString() => Value;
}
