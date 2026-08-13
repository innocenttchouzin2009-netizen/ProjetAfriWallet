namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public interface IProviderSecretSource
{
    string GetRequired(string key);
}