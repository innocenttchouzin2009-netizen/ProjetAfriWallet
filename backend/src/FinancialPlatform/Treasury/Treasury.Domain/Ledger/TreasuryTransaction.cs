namespace Treasury.Domain.Ledger;

public sealed class TreasuryTransaction
{
    private readonly List<TreasuryEntry> _entries = [];

    public TreasuryTransaction(Guid transactionId, string reference, string correlationId)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction ID is required.");

        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required.");

        TransactionId = transactionId;
        Reference = reference.Trim();
        CorrelationId = correlationId.Trim();
    }

    public Guid TransactionId { get; }
    public string Reference { get; }
    public string CorrelationId { get; }
    public TreasuryTransactionStatus Status { get; private set; } = TreasuryTransactionStatus.Pending;
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
    public DateTime? PostedAtUtc { get; private set; }
    public IReadOnlyCollection<TreasuryEntry> Entries => _entries.AsReadOnly();

    public void AddDebit(Guid accountId, string currencyCode, long amountMinor)
    {
        EnsurePending();
        _entries.Add(TreasuryEntry.Debit(TransactionId, accountId, currencyCode, amountMinor, Reference));
    }

    public void AddCredit(Guid accountId, string currencyCode, long amountMinor)
    {
        EnsurePending();
        _entries.Add(TreasuryEntry.Credit(TransactionId, accountId, currencyCode, amountMinor, Reference));
    }

    public void Post()
    {
        EnsurePending();

        if (_entries.Count < 2)
            throw new InvalidOperationException("A treasury transaction requires at least two entries.");

        var currencies = _entries.Select(x => x.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        foreach (var currency in currencies)
        {
            var currencyEntries = _entries.Where(x => string.Equals(x.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase));
            var debits = currencyEntries.Sum(x => x.DebitMinor);
            var credits = currencyEntries.Sum(x => x.CreditMinor);

            if (debits != credits)
                throw new InvalidOperationException($"Treasury transaction is unbalanced for {currency}. Debits={debits}, Credits={credits}.");
        }

        Status = TreasuryTransactionStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != TreasuryTransactionStatus.Pending)
            throw new InvalidOperationException("Posted treasury transaction is immutable.");
    }
}

public enum TreasuryTransactionStatus
{
    Pending,
    Posted
}
