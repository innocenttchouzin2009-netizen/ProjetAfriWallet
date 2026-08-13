using AfriWallet.PaymentPlatform.ProviderIntegration.Application;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Credentials;

public sealed class SandboxCredentialService : IProviderCredentialService
{
    public Task<ProviderCredential> GetCredentialAsync(
        string providerCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(providerCode))
            throw new ArgumentException("Provider code is required.", nameof(providerCode));

        return Task.FromResult(new ProviderCredential(
            $"sandbox-token-{providerCode.ToLowerInvariant()}",
            DateTimeOffset.UtcNow.AddMinutes(30)));
    }
}