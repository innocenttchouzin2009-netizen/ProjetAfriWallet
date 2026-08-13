using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Infrastructure.Ledger;

public sealed class InMemoryLedgerRepository : ILedgerRepository, ILedgerProjectionReader
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, LedgerTransaction> _transactions = new();
	private readonly Dictionary<Guid, List<LedgerEntry>> _entriesByTransactionId = new();
	private readonly Dictionary<Guid, List<LedgerEntry>> _entriesByWalletId = new();
	private readonly Dictionary<string, Guid> _idempotencyKeys = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<Guid, PostingResult> _resultsByTransactionId = new();
	private long _positionCounter;

	public bool TransactionExists(Guid transactionId)
	{
		lock (_sync)
		{
			return _transactions.ContainsKey(transactionId);
		}
	}

	public long ReserveNextPosition()
	{
		lock (_sync)
		{
			_positionCounter++;
			return _positionCounter;
		}
	}

	public IReadOnlyList<LedgerTransaction> GetTransactions()
	{
		lock (_sync)
		{
			return _transactions.Values.OrderByDescending(transaction => transaction.PostedAt).ToList();
		}
	}

	public bool TryGetByIdempotencyKey(string idempotencyKey, out LedgerTransaction transaction)
	{
		lock (_sync)
		{
			if (_idempotencyKeys.TryGetValue(idempotencyKey, out var transactionId) && _transactions.TryGetValue(transactionId, out var found))
			{
				transaction = found;
				return true;
			}

			transaction = null!;
			return false;
		}
	}

	public LedgerTransaction? GetTransaction(Guid transactionId)
	{
		lock (_sync)
		{
			return _transactions.GetValueOrDefault(transactionId);
		}
	}

	public IReadOnlyList<LedgerEntry> GetEntriesByTransaction(Guid transactionId)
	{
		lock (_sync)
		{
			return _entriesByTransactionId.GetValueOrDefault(transactionId, []).OrderBy(entry => entry.CreatedAt).ToList();
		}
	}

	public IReadOnlyList<LedgerEntry> GetEntriesByWallet(Guid walletId)
	{
		lock (_sync)
		{
			return _entriesByWalletId.GetValueOrDefault(walletId, []).OrderByDescending(entry => entry.CreatedAt).ToList();
		}
	}

	public PostingResult? GetPostingResult(Guid transactionId)
	{
		lock (_sync)
		{
			return _resultsByTransactionId.GetValueOrDefault(transactionId);
		}
	}

	public void StorePostedTransaction(LedgerTransaction transaction, IReadOnlyList<LedgerEntry> entries, string idempotencyKey, PostingResult result)
	{
		lock (_sync)
		{
			_transactions[transaction.TransactionId] = transaction;
			_entriesByTransactionId[transaction.TransactionId] = entries.ToList();
			_resultsByTransactionId[transaction.TransactionId] = result;
			_idempotencyKeys[idempotencyKey] = transaction.TransactionId;

			foreach (var entry in entries)
			{
				if (!_entriesByWalletId.TryGetValue(entry.WalletId, out var walletEntries))
				{
					walletEntries = [];
					_entriesByWalletId[entry.WalletId] = walletEntries;
				}

				walletEntries.Add(entry);
			}
		}
	}

	public void StoreRejectedTransaction(LedgerTransaction transaction, string idempotencyKey, PostingResult result)
	{
		lock (_sync)
		{
			_transactions[transaction.TransactionId] = transaction;
			_entriesByTransactionId[transaction.TransactionId] = [];
			_resultsByTransactionId[transaction.TransactionId] = result;
			_idempotencyKeys[idempotencyKey] = transaction.TransactionId;
		}
	}

	public long GetLatestPosition(Guid walletId)
	{
		lock (_sync)
		{
			var entries = _entriesByWalletId.GetValueOrDefault(walletId, []);
			return entries.Count == 0 ? 0 : entries.Max(entry => entry.Position);
		}
	}

	public IReadOnlyList<LedgerProjectionRecord> ReadWalletEntries(Guid walletId, long fromExclusivePosition)
	{
		lock (_sync)
		{
			var entries = _entriesByWalletId.GetValueOrDefault(walletId, [])
				.Where(entry => entry.Position > fromExclusivePosition)
				.OrderBy(entry => entry.Position)
				.Select(entry => new LedgerProjectionRecord(
					entry.WalletId,
					entry.Position,
					entry.EntryType == EntryType.Credit ? entry.Credit : -entry.Debit,
					entry.Currency,
					entry.Compartment,
					entry.CreatedAt))
				.ToList();

			return entries;
		}
	}
}
