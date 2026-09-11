namespace AfriWallet.PaymentRequests.Domain;

public readonly record struct PaymentRequestId
{
    public Guid Value { get; }

    private PaymentRequestId(Guid value) => Value = value;

    public static PaymentRequestId New() => new(Guid.NewGuid());

    public static PaymentRequestId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(value));
        }

        return new PaymentRequestId(value);
    }

    public override string ToString() => Value.ToString();
}
