namespace Subscriptions.Api.Configuration;

public static class SecretProviderExtensions
{
    public static string GetRequiredSecret(this ISecretProvider secretProvider, string key)
    {
        var value = secretProvider.GetSecret(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required secret '{key}'.");
        }

        return value;
    }
}
