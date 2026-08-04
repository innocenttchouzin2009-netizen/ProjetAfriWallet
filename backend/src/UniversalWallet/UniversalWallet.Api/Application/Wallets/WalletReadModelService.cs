using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Application.Wallets;

public sealed class WalletReadModelService
{
	private readonly IWalletRepository _walletRepository;
	private readonly ILedgerRepository _ledgerRepository;
	private readonly BalanceProjectionService _balanceProjectionService;

	public WalletReadModelService(
		IWalletRepository walletRepository,
		ILedgerRepository ledgerRepository,
		BalanceProjectionService balanceProjectionService)
	{
		_walletRepository = walletRepository;
		_ledgerRepository = ledgerRepository;
		_balanceProjectionService = balanceProjectionService;
	}

	public WalletDetail GetDetail(Guid walletId)
	{
		var wallet = _walletRepository.GetById(walletId);
		if (wallet is null)
		{
			throw new InvalidOperationException("WALLET_NOT_FOUND");
		}

		var projectionState = _balanceProjectionService.GetProjectionState(walletId);
		var ledgerEntries = _ledgerRepository.GetEntriesByWallet(walletId);
		var timeline = ledgerEntries
			.Select(entry => new WalletTimelineItem(
				entry.TransactionId,
				entry.Reference,
				entry.Description,
				entry.CreatedAt,
				entry.EntryType == EntryType.Credit ? "+" : "-",
				entry.EntryType == EntryType.Credit ? entry.Credit : entry.Debit,
				entry.Currency))
			.ToList();

		return new WalletDetail(
			wallet.Id,
			wallet.WalletNumber,
			wallet.Currency,
			wallet.WalletType.ToString(),
			wallet.Status.ToString(),
			projectionState.Projection.AvailableBalance,
			projectionState.Projection.PendingBalance,
			projectionState.Projection.ReservedBalance,
			projectionState.Projection.LedgerBalance,
			projectionState.Projection.UpdatedAt,
			timeline,
			timeline.FirstOrDefault()?.OccurredAt ?? wallet.UpdatedAt);
	}

	public WalletPortfolioSummary GetPortfolioSummary(string awid)
	{
		var wallets = _walletRepository.ListByAwid(awid);
		var details = wallets.Select(wallet => GetDetail(wallet.Id)).ToList();
		var totalAvailable = details.Sum(detail => detail.AvailableBalance);
		var totalLedger = details.Sum(detail => detail.LedgerBalance);
		return new WalletPortfolioSummary(
			awid,
			wallets.Count,
			totalAvailable,
			totalLedger,
			details.Select(detail => detail.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
			DateTimeOffset.UtcNow);
	}
}

public sealed record WalletDetail(
	Guid Id,
	string WalletNumber,
	string Currency,
	string WalletType,
	string Status,
	decimal AvailableBalance,
	decimal PendingBalance,
	decimal ReservedBalance,
	decimal LedgerBalance,
	DateTimeOffset UpdatedAt,
	IReadOnlyList<WalletTimelineItem> Timeline,
	DateTimeOffset LastActivityAt);

public sealed record WalletTimelineItem(
	Guid TransactionId,
	string Reference,
	string Description,
	DateTimeOffset OccurredAt,
	string Direction,
	decimal Amount,
	string Currency);

public sealed record WalletPortfolioSummary(
	string Awid,
	int WalletCount,
	decimal TotalAvailable,
	decimal TotalLedgerBalance,
	int CurrencyCount,
	DateTimeOffset GeneratedAt);
