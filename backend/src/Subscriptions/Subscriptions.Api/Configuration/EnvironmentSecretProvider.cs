namespace Subscriptions.Api.Configuration;

public sealed class EnvironmentSecretProvider : ISecretProvider
{
    public string? GetSecret(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Environment.GetEnvironmentVariable(key);
    }
}
