namespace UniversalWallet.Api.Domain.Ledger;

public sealed class LedgerEntry
{
	public LedgerEntry(
		Guid entryId,
		Guid journalId,
		Guid walletId,
		Guid transactionId,
		EntryType entryType,
		decimal debit,
		decimal credit,
		string currency,
		string reference,
		string description,
		DateTimeOffset createdAt,
		string postedBy,
		string correlationId,
		string awid,
		string session,
		string device,
		string idempotencyKey)
	{
		EntryId = entryId;
		JournalId = journalId;
		WalletId = walletId;
		TransactionId = transactionId;
		EntryType = entryType;
		Debit = debit;
		Credit = credit;
		Currency = currency;
		Reference = reference;
		Description = description;
		CreatedAt = createdAt;
		PostedBy = postedBy;
		CorrelationId = correlationId;
		Awid = awid;
		Session = session;
		Device = device;
		IdempotencyKey = idempotencyKey;
	}

	public Guid EntryId { get; }
	public Guid JournalId { get; }
	public Guid WalletId { get; }
	public Guid TransactionId { get; }
	public EntryType EntryType { get; }
	public decimal Debit { get; }
	public decimal Credit { get; }
	public string Currency { get; }
	public string Reference { get; }
	public string Description { get; }
	public DateTimeOffset CreatedAt { get; }
	public string PostedBy { get; }
	public string CorrelationId { get; }
	public string Awid { get; }
	public string Session { get; }
	public string Device { get; }
	public string IdempotencyKey { get; }
}
