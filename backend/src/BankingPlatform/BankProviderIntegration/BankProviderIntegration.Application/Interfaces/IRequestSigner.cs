namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

public interface IRequestSigner
{
    string Sign(string payload, string secret);
}
