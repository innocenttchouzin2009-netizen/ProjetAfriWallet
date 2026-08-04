namespace MobileMoney.Production.Secrets;

public sealed class EnvironmentSecretProvider : ISecretProvider
{
    public string? GetSecret(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Environment.GetEnvironmentVariable(key);
    }

    public bool HasSecret(string key)
    {
        return !string.IsNullOrWhiteSpace(GetSecret(key));
    }
}
