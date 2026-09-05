namespace UniversalWallet.Api.Domain.Ledger;

public sealed class PostingResult
{
	public PostingResult(
		bool accepted,
		LedgerTransaction? transaction,
		IReadOnlyList<LedgerEntry> entries,
		IReadOnlyList<LedgerEventType> events,
		string? code = null,
		string? message = null)
	{
		Accepted = accepted;
		Transaction = transaction;
		Entries = entries;
		Events = events;
		Code = code;
		Message = message;
	}

	public bool Accepted { get; }
	public LedgerTransaction? Transaction { get; }
	public IReadOnlyList<LedgerEntry> Entries { get; }
	public IReadOnlyList<LedgerEventType> Events { get; }
	public string? Code { get; }
	public string? Message { get; }

	public static PostingResult Success(LedgerTransaction transaction, IReadOnlyList<LedgerEntry> entries, LedgerEventType successEvent) =>
		new(true, transaction, entries, [LedgerEventType.TransactionPosted, successEvent], null, null);

	public static PostingResult Rejected(string code, string message, LedgerEventType rejectionEvent) =>
		new(false, null, [], [LedgerEventType.TransactionRejected, rejectionEvent], code, message);
}
