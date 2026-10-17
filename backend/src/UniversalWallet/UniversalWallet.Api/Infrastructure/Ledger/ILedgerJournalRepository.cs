using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Infrastructure.Ledger;

public interface ILedgerJournalRepository
{
	LedgerJournal GetOrCreateJournal(Guid walletId, string awid, string currency);
	LedgerJournal? GetByWalletId(Guid walletId);
}
