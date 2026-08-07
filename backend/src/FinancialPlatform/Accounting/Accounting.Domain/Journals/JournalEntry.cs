using Accounting.Domain.Entries;

namespace Accounting.Domain.Journals;

public enum JournalEntryStatus
{
    Draft,
    Posted
}

public sealed class JournalEntry
{
    private readonly List<GeneralLedgerEntry> _entries = new();

    public Guid JournalEntryId { get; }
    public Guid PeriodId { get; }
    public string Reference { get; }
    public string Description { get; }
    public Guid? SourceJournalEntryId { get; }
    public JournalEntryStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? PostedAtUtc { get; private set; }
    public IReadOnlyCollection<GeneralLedgerEntry> Entries => _entries.AsReadOnly();

    public JournalEntry(
        Guid journalEntryId,
        Guid periodId,
        string reference,
        string description,
        Guid? sourceJournalEntryId = null,
        DateTime? createdAtUtc = null)
    {
        if (journalEntryId == Guid.Empty)
            throw new ArgumentException("Journal entry identifier is required.", nameof(journalEntryId));

        if (periodId == Guid.Empty)
            throw new ArgumentException("Period identifier is required.", nameof(periodId));

        JournalEntryId = journalEntryId;
        PeriodId = periodId;
        Reference = RequireText(reference, nameof(reference));
        Description = RequireText(description, nameof(description));
        SourceJournalEntryId = sourceJournalEntryId;
        Status = JournalEntryStatus.Draft;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public void AddDebit(Guid accountId, string currencyCode, long amountMinor, string? narration = null)
    {
        EnsureMutable();
        _entries.Add(new GeneralLedgerEntry(Guid.NewGuid(), JournalEntryId, accountId, currencyCode, amountMinor, JournalLineSide.Debit, narration, CreatedAtUtc));
    }

    public void AddCredit(Guid accountId, string currencyCode, long amountMinor, string? narration = null)
    {
        EnsureMutable();
        _entries.Add(new GeneralLedgerEntry(Guid.NewGuid(), JournalEntryId, accountId, currencyCode, amountMinor, JournalLineSide.Credit, narration, CreatedAtUtc));
    }

    public void Post(DateTime postedAtUtc)
    {
        EnsureMutable();

        if (_entries.Count == 0)
            throw new InvalidOperationException("Journal entry must contain at least one line.");

        var debitMinor = _entries.Sum(x => x.DebitMinor);
        var creditMinor = _entries.Sum(x => x.CreditMinor);

        if (debitMinor != creditMinor)
            throw new InvalidOperationException("Journal entry must be balanced.");

        Status = JournalEntryStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    private void EnsureMutable()
    {
        if (Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Posted journal entries are immutable.");
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        return value.Trim();
    }
}