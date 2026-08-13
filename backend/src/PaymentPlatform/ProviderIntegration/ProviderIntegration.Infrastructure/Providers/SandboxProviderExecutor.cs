using AfriWallet.PaymentPlatform.ProviderIntegration.Application;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Providers;

public sealed class SandboxProviderExecutor : IProviderExecutor
{
    public Task<ProviderExecutionResult> ExecuteAsync(
        ProviderExecutionRequest request,
        ProviderCredential credential,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Operation.Equals(
                "FAIL_RETRYABLE",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ProviderExecutionResult(
                false,
                null,
                "temporary_provider_failure",
                "Temporary sandbox provider failure.",
                true));
        }

        if (request.Operation.Equals(
                "FAIL_FINAL",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ProviderExecutionResult(
                false,
                null,
                "provider_rejected",
                "Sandbox provider rejected the operation.",
                false));
        }

        return Task.FromResult(new ProviderExecutionResult(
            true,
            $"provider-ref-{Guid.NewGuid():N}",
            null,
            null,
            false));
    }
}