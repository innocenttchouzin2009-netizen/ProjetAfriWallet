using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Infrastructure.Ledger;

public interface ILedgerRepository
{
	bool TransactionExists(Guid transactionId);
	IReadOnlyList<LedgerTransaction> GetTransactions();
	bool TryGetByIdempotencyKey(string idempotencyKey, out LedgerTransaction transaction);
	LedgerTransaction? GetTransaction(Guid transactionId);
	IReadOnlyList<LedgerEntry> GetEntriesByTransaction(Guid transactionId);
	IReadOnlyList<LedgerEntry> GetEntriesByWallet(Guid walletId);
	PostingResult? GetPostingResult(Guid transactionId);
	void StorePostedTransaction(LedgerTransaction transaction, IReadOnlyList<LedgerEntry> entries, string idempotencyKey, PostingResult result);
	void StoreRejectedTransaction(LedgerTransaction transaction, string idempotencyKey, PostingResult result);
}
