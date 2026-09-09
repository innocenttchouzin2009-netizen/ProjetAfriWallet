namespace AfriWallet.Ledger.Domain;

public sealed class JournalEntry
{
    private readonly LedgerLine[] lines;

    public JournalEntryId Id { get; }
    public string CurrencyCode { get; }
    public string BusinessReference { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset PostedAtUtc { get; }
    public IReadOnlyList<LedgerLine> Lines => lines;

    private JournalEntry(
        JournalEntryId id,
        string currencyCode,
        string businessReference,
        Guid correlationId,
        DateTimeOffset postedAtUtc,
        LedgerLine[] lines)
    {
        Id = id;
        CurrencyCode = currencyCode;
        BusinessReference = businessReference;
        CorrelationId = correlationId;
        PostedAtUtc = postedAtUtc;
        this.lines = lines;
    }

    public static JournalEntry Create(
        JournalEntryId id,
        string currencyCode,
        string businessReference,
        Guid correlationId,
        DateTimeOffset postedAtUtc,
        IEnumerable<LedgerLine> lines)
    {
        var normalizedCurrency = NormalizeCurrency(currencyCode);
        var normalizedReference = NormalizeReference(businessReference);

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        if (postedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Posted timestamp must be UTC.", nameof(postedAtUtc));
        }

        ArgumentNullException.ThrowIfNull(lines);
        var materialized = lines.ToArray();
        ValidateBalanced(materialized);

        return new JournalEntry(
            id,
            normalizedCurrency,
            normalizedReference,
            correlationId,
            postedAtUtc,
            materialized);
    }

    private static void ValidateBalanced(IReadOnlyCollection<LedgerLine> lines)
    {
        if (lines.Count < 2)
        {
            throw new InvalidOperationException("A journal entry requires at least two ledger lines.");
        }

        long debit = 0;
        long credit = 0;

        checked
        {
            foreach (var line in lines)
            {
                if (line.Side == LedgerSide.Debit)
                {
                    debit += line.AmountMinor;
                }
                else if (line.Side == LedgerSide.Credit)
                {
                    credit += line.AmountMinor;
                }
                else
                {
                    throw new InvalidOperationException("Journal entry contains an invalid ledger side.");
                }
            }
        }

        if (debit == 0 || credit == 0 || debit != credit)
        {
            throw new InvalidOperationException("Journal entry is not balanced: total debits must equal total credits.");
        }
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ISO-like letters.", nameof(currencyCode));
        }

        return normalized;
    }

    private static string NormalizeReference(string businessReference)
    {
        if (string.IsNullOrWhiteSpace(businessReference))
        {
            throw new ArgumentException("Business reference is required.", nameof(businessReference));
        }

        var normalized = businessReference.Trim();
        if (normalized.Length > 128)
        {
            throw new ArgumentException("Business reference cannot exceed 128 characters.", nameof(businessReference));
        }

        return normalized;
    }
}
