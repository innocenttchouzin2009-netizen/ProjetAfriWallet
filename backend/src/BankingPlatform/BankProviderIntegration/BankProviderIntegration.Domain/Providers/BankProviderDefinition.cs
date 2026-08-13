namespace AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;

public sealed class BankProviderDefinition
{
    private readonly HashSet<BankProviderCapability> _capabilities;

    public BankProviderDefinition(
        string providerCode,
        string displayName,
        IEnumerable<BankProviderCapability> capabilities)
    {
        ProviderCode = Require(providerCode).ToUpperInvariant();
        DisplayName = Require(displayName);
        _capabilities = capabilities.ToHashSet();

        if (_capabilities.Count == 0)
            throw new ArgumentException(
                "At least one provider capability is required.");

        Environment = BankProviderEnvironment.Sandbox;
    }

    public string ProviderCode { get; }

    public string DisplayName { get; }

    public BankProviderEnvironment Environment { get; }

    public bool Enabled { get; private set; } = true;

    public IReadOnlySet<BankProviderCapability> Capabilities => _capabilities;

    public bool Supports(BankProviderCapability capability) =>
        Enabled && _capabilities.Contains(capability);

    public void Disable() => Enabled = false;

    public void Enable() => Enabled = true;

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }
}
