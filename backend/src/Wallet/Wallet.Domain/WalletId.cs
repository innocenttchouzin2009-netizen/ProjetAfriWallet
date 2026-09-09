namespace AfriWallet.Wallet.Domain;

public readonly record struct WalletId
{
    public Guid Value { get; }

    private WalletId(Guid value) => Value = value;

    public static WalletId New() => new(Guid.NewGuid());

    public static WalletId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Wallet id cannot be empty.", nameof(value));
        }

        return new WalletId(value);
    }

    public override string ToString() => Value.ToString();
}
