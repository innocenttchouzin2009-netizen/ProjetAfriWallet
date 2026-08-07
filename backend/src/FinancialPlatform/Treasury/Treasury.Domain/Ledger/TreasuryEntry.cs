namespace Treasury.Domain.Ledger;

public sealed class TreasuryEntry
{
    public TreasuryEntry(
        string accountId,
        decimal amount,
        string side)
    {
        AccountId = accountId;
        Amount = amount;
        Side = side;
    }

    public string AccountId { get; }

    public decimal Amount { get; }

    public string Side { get; }
}
