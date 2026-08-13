using AfriWallet.PaymentPlatform.ProviderIntegration.Application;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Secrets;

public sealed class EnvironmentSecretSource : IProviderSecretSource
{
    public string GetRequired(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Secret key is required.", nameof(key));

        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required secret '{key}' is not configured.");
        }

        return value;
    }
}