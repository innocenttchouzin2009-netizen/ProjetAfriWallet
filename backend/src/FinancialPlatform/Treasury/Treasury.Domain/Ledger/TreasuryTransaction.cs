namespace Treasury.Domain.Ledger;

public sealed class TreasuryTransaction
{
    public TreasuryTransaction(
        string transactionId,
        IReadOnlyCollection<TreasuryEntry> entries,
        DateTime timestampUtc)
    {
        TransactionId = transactionId;
        Entries = entries;
        TimestampUtc = timestampUtc;
    }

    public string TransactionId { get; }

    public IReadOnlyCollection<TreasuryEntry> Entries { get; }

    public DateTime TimestampUtc { get; }
}
