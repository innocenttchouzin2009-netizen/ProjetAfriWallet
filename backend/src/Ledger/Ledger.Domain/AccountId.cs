namespace AfriWallet.Ledger.Domain;

public readonly record struct AccountId
{
    public Guid Value { get; }

    public AccountId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Account id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static AccountId New() => new(Guid.NewGuid());
}
