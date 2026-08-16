using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

namespace AfriWallet.Compliance.IdentityVerification.Infrastructure;

public sealed class VerificationProviderRegistry : IVerificationProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IVerificationProvider> _providers;

    public VerificationProviderRegistry(IEnumerable<IVerificationProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Descriptor.Code, StringComparer.OrdinalIgnoreCase);
    }

    public IVerificationProvider Resolve(string providerCode)
    {
        if (!_providers.TryGetValue(providerCode, out var provider))
        {
            throw new KeyNotFoundException($"Verification provider '{providerCode}' not found.");
        }

        return provider;
    }

    public IReadOnlyCollection<IVerificationProvider> All() => _providers.Values.ToArray();
}
