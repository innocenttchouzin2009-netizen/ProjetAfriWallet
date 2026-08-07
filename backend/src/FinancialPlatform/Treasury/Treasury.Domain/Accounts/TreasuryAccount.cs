namespace Treasury.Domain.Accounts;

public sealed class TreasuryAccount
{
    public TreasuryAccount(
        string accountId,
        string name,
        string currency)
    {
        AccountId = accountId;
        Name = name;
        Currency = currency;
    }

    public string AccountId { get; }

    public string Name { get; }

    public string Currency { get; }
}
