namespace UniversalWallet.Api.Domain.Ledger;

public enum LedgerJournalStatus
{
	Active,
	Closed
}

public sealed class LedgerJournal
{
	public LedgerJournal(Guid journalId, string name, string currency, LedgerJournalStatus status, DateTimeOffset createdAt)
	{
		JournalId = journalId;
		Name = name;
		Currency = currency;
		Status = status;
		CreatedAt = createdAt;
	}

	public Guid JournalId { get; }
	public string Name { get; }
	public string Currency { get; }
	public LedgerJournalStatus Status { get; }
	public DateTimeOffset CreatedAt { get; }
}
