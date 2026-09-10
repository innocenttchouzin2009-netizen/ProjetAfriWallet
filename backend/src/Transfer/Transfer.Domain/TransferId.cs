namespace AfriWallet.Transfer.Domain;

public readonly record struct TransferId
{
    public Guid Value { get; }

    public TransferId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Transfer id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static TransferId New() => new(Guid.NewGuid());
}
