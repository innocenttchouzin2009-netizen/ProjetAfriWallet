namespace AfriWallet.Wallet.Application;

public interface ISupportedCurrencyPolicy
{
    bool IsSupported(string currencyCode);
}
