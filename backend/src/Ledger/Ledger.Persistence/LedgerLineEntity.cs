namespace AfriWallet.Ledger.Persistence;

public sealed class LedgerLineEntity
{
    public Guid JournalEntryId { get; set; }
    public int Position { get; set; }
    public Guid AccountId { get; set; }
    public int Side { get; set; }
    public long AmountMinor { get; set; }
    public string? Memo { get; set; }
    public LedgerJournalEntity JournalEntry { get; set; } = null!;
}
