using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;
using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Adapters;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Registries;

public sealed class InMemoryBankProviderRegistry : IBankProviderRegistry
{
    private readonly Dictionary<string, BankProviderDefinition> _providers;
    private readonly Dictionary<string, IBankProviderAdapter> _adapters;

    public InMemoryBankProviderRegistry()
    {
        var sandboxSepa = new BankProviderDefinition(
            "SEPA-SANDBOX",
            "Sandbox SEPA",
            new[]
            {
                BankProviderCapability.SepaCreditTransfer,
                BankProviderCapability.TransferStatus,
                BankProviderCapability.Webhooks,
                BankProviderCapability.Reconciliation
            });

        _providers = new Dictionary<string, BankProviderDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [sandboxSepa.ProviderCode] = sandboxSepa
        };

        _adapters = new Dictionary<string, IBankProviderAdapter>(StringComparer.OrdinalIgnoreCase)
        {
            [sandboxSepa.ProviderCode] = new SandboxSepaBankAdapter()
        };
    }

    public BankProviderDefinition GetRequired(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new ArgumentException("Provider code is required.", nameof(providerCode));

        if (_providers.TryGetValue(providerCode, out var provider))
            return provider;

        throw new KeyNotFoundException($"Provider '{providerCode}' was not found.");
    }

    public IBankProviderAdapter GetAdapter(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new ArgumentException("Provider code is required.", nameof(providerCode));

        if (_adapters.TryGetValue(providerCode, out var adapter))
            return adapter;

        throw new KeyNotFoundException($"Provider adapter '{providerCode}' was not found.");
    }

    public IReadOnlyCollection<BankProviderDefinition> List() =>
        _providers.Values.ToArray();
}
