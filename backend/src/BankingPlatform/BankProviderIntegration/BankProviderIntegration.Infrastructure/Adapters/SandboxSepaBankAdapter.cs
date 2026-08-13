using AfriWallet.BankingPlatform.BankProviderIntegration.Application;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;
using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Adapters;

public sealed class SandboxSepaBankAdapter : IBankProviderAdapter
{
    public string ProviderCode => "SEPA-SANDBOX";

    public Task<ProviderSubmission> SubmitAsync(
        SubmitProviderTransferRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new ProviderSubmission(
            true,
            $"SEPA-SBX-{Guid.NewGuid():N}",
            null,
            false));
    }

    public Task<BankProviderHealth> CheckHealthAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new BankProviderHealth(
            ProviderCode,
            true,
            "sandbox-healthy",
            DateTime.UtcNow));
    }
}
