namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public interface IProviderCredentialService
{
    Task<ProviderCredential> GetCredentialAsync(
        string providerCode,
        CancellationToken cancellationToken = default);
}