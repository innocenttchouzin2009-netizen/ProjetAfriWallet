namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public interface IProviderWebhookVerifier
{
    bool Verify(ProviderWebhookVerificationRequest request);
}