namespace AfriWallet.Ledger.Domain;

public readonly record struct JournalEntryId
{
    public Guid Value { get; }

    public JournalEntryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Journal entry id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static JournalEntryId New() => new(Guid.NewGuid());
}
