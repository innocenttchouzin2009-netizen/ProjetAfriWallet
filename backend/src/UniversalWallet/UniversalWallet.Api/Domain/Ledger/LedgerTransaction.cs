namespace UniversalWallet.Api.Domain.Ledger;

public enum LedgerTransactionStatus
{
	Posted,
	Rejected,
	Reversed
}

public sealed class LedgerTransaction
{
	public LedgerTransaction(
		Guid transactionId,
		LedgerTransactionStatus status,
		string currency,
		DateTimeOffset postedAt,
		string reference,
		string correlationId,
		string postedBy,
		string awid,
		string session,
		string device,
		string idempotencyKey,
		Guid? reversalOfTransactionId = null)
	{
		TransactionId = transactionId;
		Status = status;
		Currency = currency;
		PostedAt = postedAt;
		Reference = reference;
		CorrelationId = correlationId;
		PostedBy = postedBy;
		Awid = awid;
		Session = session;
		Device = device;
		IdempotencyKey = idempotencyKey;
		ReversalOfTransactionId = reversalOfTransactionId;
	}

	public Guid TransactionId { get; }
	public LedgerTransactionStatus Status { get; }
	public string Currency { get; }
	public DateTimeOffset PostedAt { get; }
	public string Reference { get; }
	public string CorrelationId { get; }
	public string PostedBy { get; }
	public string Awid { get; }
	public string Session { get; }
	public string Device { get; }
	public string IdempotencyKey { get; }
	public Guid? ReversalOfTransactionId { get; }
}
