using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Telemetry;

public sealed class NoOpProviderTelemetry : IProviderTelemetry
{
    public void SubmissionStarted(string providerCode) { }

    public void SubmissionSucceeded(string providerCode) { }

    public void SubmissionFailed(string providerCode, string errorCode) { }

    public void WebhookAccepted(string providerCode) { }

    public void WebhookRejected(string providerCode) { }
}
