namespace Treasury.Domain.Balances;

public sealed class TreasuryBalance
{
    public TreasuryBalance(
        string accountId,
        decimal available,
        decimal reserved)
    {
        AccountId = accountId;
        Available = available;
        Reserved = reserved;
    }

    public string AccountId { get; }

    public decimal Available { get; private set; }

    public decimal Reserved { get; private set; }

    public decimal Total => Available + Reserved;

    public void ApplyDebit(decimal amount)
    {
        Available -= amount;
    }

    public void ApplyCredit(decimal amount)
    {
        Available += amount;
    }

    public void Reserve(decimal amount)
    {
        Available -= amount;
        Reserved += amount;
    }

    public void Release(decimal amount)
    {
        Reserved -= amount;
        Available += amount;
    }
}
