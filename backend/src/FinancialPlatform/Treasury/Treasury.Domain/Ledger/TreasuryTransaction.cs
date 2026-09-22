namespace Treasury.Domain.Ledger;

public sealed class TreasuryTransaction
{
    private readonly List<TreasuryEntry> _entries = [];

    public TreasuryTransaction(Guid transactionId, string reference, string correlationId)
        : this(transactionId, reference, correlationId, TreasuryTransactionStatus.Pending, DateTime.UtcNow, null, [])
    {
    }

    private TreasuryTransaction(Guid transactionId, string reference, string correlationId, TreasuryTransactionStatus status, DateTime createdAtUtc, DateTime? postedAtUtc, IEnumerable<TreasuryEntry> entries)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction ID is required.");

        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required.");

        TransactionId = transactionId;
        Reference = reference.Trim();
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID is required.");

        CorrelationId = correlationId.Trim();
        Status = status;
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        PostedAtUtc = postedAtUtc is null ? null : DateTime.SpecifyKind(postedAtUtc.Value, DateTimeKind.Utc);
        _entries.AddRange(entries);
    }

    public static TreasuryTransaction Restore(
        Guid transactionId,
        string reference,
        string correlationId,
        TreasuryTransactionStatus status,
        DateTime createdAtUtc,
        DateTime? postedAtUtc,
        IEnumerable<TreasuryEntry> entries) =>
        new(transactionId, reference, correlationId, status, createdAtUtc, postedAtUtc, entries);

    public Guid TransactionId { get; }
    public string Reference { get; }
    public string CorrelationId { get; }
    public TreasuryTransactionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
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
