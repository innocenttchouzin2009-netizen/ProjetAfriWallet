using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Services;

public sealed class BankWebhookService
{
    private readonly IBankProviderRegistry _providers;
    private readonly IWebhookVerifier _verifier;
    private readonly IProviderTelemetry _telemetry;

    public BankWebhookService(
        IBankProviderRegistry providers,
        IWebhookVerifier verifier,
        IProviderTelemetry telemetry)
    {
        _providers = providers;
        _verifier = verifier;
        _telemetry = telemetry;
    }

    public ProviderWebhookResult Process(
        ProviderWebhookRequest request,
        string sandboxSecret)
    {
        var provider = _providers.GetRequired(request.ProviderCode);

        if (!provider.Supports(Domain.Providers.BankProviderCapability.Webhooks))
        {
            throw new InvalidOperationException(
                "Provider does not support webhooks.");
        }

        var valid = _verifier.Verify(
            request.Payload,
            request.Signature,
            sandboxSecret);

        if (!valid)
        {
            _telemetry.WebhookRejected(request.ProviderCode);
            return new ProviderWebhookResult(
                false,
                request.ProviderCode,
                "rejected");
        }

        _telemetry.WebhookAccepted(request.ProviderCode);
        return new ProviderWebhookResult(
            true,
            request.ProviderCode,
            "sandbox.transfer.updated");
    }
}
