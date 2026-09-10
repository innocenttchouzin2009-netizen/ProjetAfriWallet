using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Infrastructure.Ledger;

public sealed class InMemoryLedgerJournalRepository : ILedgerJournalRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, LedgerJournal> _journalsByWalletId = new();

	public LedgerJournal GetOrCreateJournal(Guid walletId, string awid, string currency)
	{
		lock (_sync)
		{
			if (_journalsByWalletId.TryGetValue(walletId, out var existing))
			{
				return existing;
			}

			var journal = new LedgerJournal(walletId, $"{awid}-{currency}-journal", currency.ToUpperInvariant(), LedgerJournalStatus.Active, DateTimeOffset.UtcNow);
			_journalsByWalletId[walletId] = journal;
			return journal;
		}
	}

	public LedgerJournal? GetByWalletId(Guid walletId)
	{
		lock (_sync)
		{
			return _journalsByWalletId.GetValueOrDefault(walletId);
		}
	}
}
