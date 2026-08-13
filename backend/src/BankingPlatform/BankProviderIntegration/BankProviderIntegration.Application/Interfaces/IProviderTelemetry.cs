namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

public interface IProviderTelemetry
{
    void SubmissionStarted(string providerCode);
    void SubmissionSucceeded(string providerCode);
    void SubmissionFailed(string providerCode, string errorCode);
    void WebhookAccepted(string providerCode);
    void WebhookRejected(string providerCode);
}
