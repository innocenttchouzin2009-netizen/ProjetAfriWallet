using UniversalWallet.Api.Domain.Balance;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Balance;

namespace UniversalWallet.Api.Application.Balance;

public sealed class BalanceProjectionService
{
	private readonly ILedgerProjectionReader _ledgerProjectionReader;
	private readonly IWalletCurrencyReader _walletCurrencyReader;
	private readonly IBalanceProjectionRepository _projectionRepository;
	private readonly IBalanceSnapshotRepository _snapshotRepository;
	private readonly IProjectionVersionRepository _versionRepository;

	public BalanceProjectionService(
		ILedgerProjectionReader ledgerProjectionReader,
		IWalletCurrencyReader walletCurrencyReader,
		IBalanceProjectionRepository projectionRepository,
		IBalanceSnapshotRepository snapshotRepository,
		IProjectionVersionRepository versionRepository)
	{
		_ledgerProjectionReader = ledgerProjectionReader;
		_walletCurrencyReader = walletCurrencyReader;
		_projectionRepository = projectionRepository;
		_snapshotRepository = snapshotRepository;
		_versionRepository = versionRepository;
	}

	public WalletBalanceProjectionState GetProjectionState(Guid walletId)
	{
		EnsureWalletExists(walletId, out var currency);
		var latestPosition = _ledgerProjectionReader.GetLatestPosition(walletId);
		var current = _projectionRepository.Get(walletId);
		if (current is null)
		{
			var rebuilt = RebuildFromLedger(walletId);
			return new WalletBalanceProjectionState(rebuilt, latestPosition, rebuilt.LastLedgerPosition == latestPosition, false);
		}

		if (!string.Equals(current.Currency, currency, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("WALLET_CURRENCY_MISMATCH");
		}

		var wasLagging = current.LastLedgerPosition < latestPosition;
		if (wasLagging)
		{
			current = ProjectIncremental(current, latestPosition);
		}

		return new WalletBalanceProjectionState(current, latestPosition, current.LastLedgerPosition == latestPosition, wasLagging);
	}

	public WalletBalanceProjection RebuildFromLedger(Guid walletId)
	{
		EnsureWalletExists(walletId, out var currency);
		var records = _ledgerProjectionReader.ReadWalletEntries(walletId, 0);
		var projection = BuildProjection(
			walletId,
			currency,
			records,
			new WalletBalanceProjection(walletId, currency, 0m, 0m, 0m, 0m, 0m, 0m, 0, DateTimeOffset.UtcNow));

		StoreProjection(walletId, projection);
		return projection;
	}

	public ProjectionVersion? GetVersion(Guid walletId) => _versionRepository.Get(walletId);

	private WalletBalanceProjection ProjectIncremental(WalletBalanceProjection current, long latestPosition)
	{
		if (current.LastLedgerPosition >= latestPosition)
		{
			return current;
		}

		var records = _ledgerProjectionReader.ReadWalletEntries(current.WalletId, current.LastLedgerPosition);
		if (records.Count == 0)
		{
			return current;
		}

		var updated = BuildProjection(current.WalletId, current.Currency, records, current);
		StoreProjection(current.WalletId, updated);
		return updated;
	}

	private WalletBalanceProjection BuildProjection(
		Guid walletId,
		string currency,
		IReadOnlyList<LedgerProjectionRecord> records,
		WalletBalanceProjection seed)
	{
		var ledgerBalance = seed.LedgerBalance;
		var available = seed.AvailableBalance;
		var reserved = seed.ReservedBalance;
		var pending = seed.PendingBalance;
		var incomingToday = seed.IncomingToday;
		var outgoingToday = seed.OutgoingToday;
		var lastPosition = seed.LastLedgerPosition;
		var today = DateTimeOffset.UtcNow.Date;

		foreach (var record in records.OrderBy(record => record.Position))
		{
			if (record.Position <= lastPosition)
			{
				continue;
			}

			if (!string.Equals(record.Currency, currency, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("WALLET_CURRENCY_MISMATCH");
			}

			ledgerBalance += record.SignedAmount;
			switch (record.Compartment)
			{
				case LedgerBalanceCompartment.Available:
					available += record.SignedAmount;
					break;
				case LedgerBalanceCompartment.Reserved:
					reserved += record.SignedAmount;
					break;
				case LedgerBalanceCompartment.Pending:
					pending += record.SignedAmount;
					break;
			}

			if (record.PostedAt.UtcDateTime.Date == today)
			{
				if (record.SignedAmount > 0)
				{
					incomingToday += record.SignedAmount;
				}
				else
				{
					outgoingToday += Math.Abs(record.SignedAmount);
				}
			}

			lastPosition = record.Position;
		}

		return new WalletBalanceProjection(
			walletId,
			currency,
			ledgerBalance,
			available,
			pending,
			reserved,
			incomingToday,
			outgoingToday,
			lastPosition,
			DateTimeOffset.UtcNow);
	}

	private void StoreProjection(Guid walletId, WalletBalanceProjection projection)
	{
		_projectionRepository.Upsert(projection);
		_snapshotRepository.Save(new BalanceSnapshot(
			walletId,
			projection.Currency,
			projection.LedgerBalance,
			projection.AvailableBalance,
			projection.PendingBalance,
			projection.ReservedBalance,
			projection.IncomingToday,
			projection.OutgoingToday,
			projection.LastLedgerPosition,
			projection.UpdatedAt));
		_versionRepository.Increment(walletId, projection.LastLedgerPosition);
	}

	private void EnsureWalletExists(Guid walletId, out string currency)
	{
		if (!_walletCurrencyReader.TryGetWalletCurrency(walletId, out currency))
		{
			throw new InvalidOperationException("WALLET_NOT_FOUND");
		}
	}
}
