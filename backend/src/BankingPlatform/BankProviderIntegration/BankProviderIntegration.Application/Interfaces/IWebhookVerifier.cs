namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

public interface IWebhookVerifier
{
    bool Verify(string payload, string signature, string secret);
}
