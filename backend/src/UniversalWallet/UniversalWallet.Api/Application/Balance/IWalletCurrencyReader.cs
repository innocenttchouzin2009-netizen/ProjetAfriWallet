namespace UniversalWallet.Api.Application.Balance;

public interface IWalletCurrencyReader
{
	bool TryGetWalletCurrency(Guid walletId, out string currency);
}
