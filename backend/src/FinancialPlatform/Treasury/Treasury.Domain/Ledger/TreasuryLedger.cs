namespace Treasury.Domain.Ledger;

public sealed class TreasuryLedger
{
    private readonly List<TreasuryTransaction> _transactions = new();

    public IReadOnlyCollection<TreasuryTransaction> Transactions => _transactions;

    public void Post(TreasuryTransaction transaction)
    {
        var debit = transaction.Entries
            .Where(x => x.Side == "DEBIT")
            .Sum(x => x.Amount);

        var credit = transaction.Entries
            .Where(x => x.Side == "CREDIT")
            .Sum(x => x.Amount);

        if (debit != credit)
        {
            throw new InvalidOperationException(
                "Double-entry validation failed.");
        }

        _transactions.Add(transaction);
    }
}
