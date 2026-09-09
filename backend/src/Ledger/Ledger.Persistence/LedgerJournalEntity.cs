namespace AfriWallet.Ledger.Persistence;

public sealed class LedgerJournalEntity
{
    public Guid Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string BusinessReference { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTimeOffset PostedAtUtc { get; set; }
    public List<LedgerLineEntity> Lines { get; set; } = [];
}
