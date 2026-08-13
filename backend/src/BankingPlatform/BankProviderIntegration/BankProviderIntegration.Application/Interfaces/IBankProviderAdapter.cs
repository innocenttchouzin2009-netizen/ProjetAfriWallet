using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

public interface IBankProviderAdapter
{
    string ProviderCode { get; }

    Task<ProviderSubmission> SubmitAsync(
        SubmitProviderTransferRequest request,
        CancellationToken cancellationToken);

    Task<BankProviderHealth> CheckHealthAsync(
        CancellationToken cancellationToken);
}
