namespace Accounting.Domain.Entries;

public enum JournalLineSide
{
    Debit,
    Credit
}

public sealed class GeneralLedgerEntry
{
    public Guid EntryId { get; }
    public Guid JournalEntryId { get; }
    public Guid AccountId { get; }
    public string CurrencyCode { get; }
    public long DebitMinor { get; }
    public long CreditMinor { get; }
    public string? Narration { get; }
    public DateTime PostedAtUtc { get; }
    public JournalLineSide Side { get; }

    public GeneralLedgerEntry(
        Guid entryId,
        Guid journalEntryId,
        Guid accountId,
        string currencyCode,
        long amountMinor,
        JournalLineSide side,
        string? narration,
        DateTime postedAtUtc)
    {
        if (entryId == Guid.Empty)
            throw new ArgumentException("Entry identifier is required.", nameof(entryId));

        if (journalEntryId == Guid.Empty)
            throw new ArgumentException("Journal entry identifier is required.", nameof(journalEntryId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account identifier is required.", nameof(accountId));

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Amount must be positive.");

        EntryId = entryId;
        JournalEntryId = journalEntryId;
        AccountId = accountId;
        CurrencyCode = RequireText(currencyCode, nameof(currencyCode)).ToUpperInvariant();
        Narration = string.IsNullOrWhiteSpace(narration) ? null : narration.Trim();
        PostedAtUtc = postedAtUtc;
        Side = side;

        if (side == JournalLineSide.Debit)
        {
            DebitMinor = amountMinor;
            CreditMinor = 0;
            return;
        }

        DebitMinor = 0;
        CreditMinor = amountMinor;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        return value.Trim();
    }
}