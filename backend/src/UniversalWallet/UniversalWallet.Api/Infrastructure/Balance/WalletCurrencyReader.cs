using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Infrastructure.Balance;

public sealed class WalletCurrencyReader : IWalletCurrencyReader
{
	private readonly IWalletRepository _walletRepository;

	public WalletCurrencyReader(IWalletRepository walletRepository)
	{
		_walletRepository = walletRepository;
	}

	public bool TryGetWalletCurrency(Guid walletId, out string currency)
	{
		var wallet = _walletRepository.GetById(walletId);
		if (wallet is null)
		{
			currency = string.Empty;
			return false;
		}

		currency = wallet.Currency;
		return true;
	}
}
