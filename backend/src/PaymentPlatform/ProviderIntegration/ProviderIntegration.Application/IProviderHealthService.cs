using AfriWallet.PaymentPlatform.ProviderIntegration.Domain;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public interface IProviderHealthService
{
    ProviderHealth Get(string providerCode);

    void RecordSuccess(string providerCode, double latencyMs);

    void RecordFailure(string providerCode, double latencyMs);
}