using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Application.Ledger;

public sealed class LedgerLineRequest
{
	public Guid WalletId { get; init; }
	public EntryType EntryType { get; init; }
	public decimal Amount { get; init; }
	public LedgerBalanceCompartment Compartment { get; init; } = LedgerBalanceCompartment.Available;
	public string Description { get; init; } = string.Empty;
}

public sealed class PostTransactionRequest
{
	public string IdempotencyKey { get; init; } = string.Empty;
	public string Awid { get; init; } = string.Empty;
	public string Currency { get; init; } = string.Empty;
	public string Reference { get; init; } = string.Empty;
	public string CorrelationId { get; init; } = string.Empty;
	public string PostedBy { get; init; } = string.Empty;
	public string Session { get; init; } = string.Empty;
	public string Device { get; init; } = string.Empty;
	public Guid? TransactionId { get; init; }
	public IReadOnlyList<LedgerLineRequest> Lines { get; init; } = [];
}

public sealed class ReverseTransactionRequest
{
	public string IdempotencyKey { get; init; } = string.Empty;
	public Guid TransactionId { get; init; }
	public string Awid { get; init; } = string.Empty;
	public string Reference { get; init; } = string.Empty;
	public string CorrelationId { get; init; } = string.Empty;
	public string PostedBy { get; init; } = string.Empty;
	public string Session { get; init; } = string.Empty;
	public string Device { get; init; } = string.Empty;
}
