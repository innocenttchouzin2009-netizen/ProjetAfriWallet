namespace AfriWallet.Balance.Domain;

public sealed record AccountBalanceSnapshot
{
    public BalanceKey Key { get; }
    public long DebitMinor { get; }
    public long CreditMinor { get; }
    public long NetMinor { get; }

    public AccountBalanceSnapshot(BalanceKey key, long debitMinor, long creditMinor)
    {
        if (debitMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(debitMinor), "Debit total cannot be negative.");
        }

        if (creditMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditMinor), "Credit total cannot be negative.");
        }

        Key = key;
        DebitMinor = debitMinor;
        CreditMinor = creditMinor;
        NetMinor = checked(creditMinor - debitMinor);
    }

    public static AccountBalanceSnapshot Zero(BalanceKey key) => new(key, 0, 0);
}
