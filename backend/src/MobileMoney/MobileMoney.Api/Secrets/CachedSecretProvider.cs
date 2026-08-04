namespace MobileMoney.Production.Secrets;

public sealed class CachedSecretProvider : ISecretProvider
{
    private readonly ISecretProvider _inner;
    private readonly Dictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public CachedSecretProvider(ISecretProvider inner)
    {
        _inner = inner;
    }

    public string? GetSecret(string key)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return value;
        }

        value = _inner.GetSecret(key);
        _cache[key] = value;
        return value;
    }

    public bool HasSecret(string key)
    {
        return !string.IsNullOrWhiteSpace(GetSecret(key));
    }
}
