namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public interface IProviderExecutor
{
    Task<ProviderExecutionResult> ExecuteAsync(
        ProviderExecutionRequest request,
        ProviderCredential credential,
        CancellationToken cancellationToken = default);
}