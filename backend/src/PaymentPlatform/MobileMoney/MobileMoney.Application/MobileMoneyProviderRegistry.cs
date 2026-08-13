using AfriWallet.PaymentPlatform.MobileMoney.Domain;

namespace AfriWallet.PaymentPlatform.MobileMoney.Application;

public sealed class MobileMoneyProviderRegistry : IMobileMoneyProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IMobileMoneyProvider> _providers;

    public MobileMoneyProviderRegistry(IEnumerable<IMobileMoneyProvider> providers)
    {
        var providerList = providers.ToArray();

        var duplicates = providerList
            .GroupBy(
                provider => provider.Definition.Code,
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate Mobile Money providers: {string.Join(", ", duplicates)}");
        }

        _providers = providerList.ToDictionary(
            provider => provider.Definition.Code,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IMobileMoneyProvider> GetAll()
        => _providers.Values.ToArray();

    public bool TryGet(
        string providerCode,
        out IMobileMoneyProvider? provider)
        => _providers.TryGetValue(providerCode, out provider);

    public IMobileMoneyProvider GetRequired(string providerCode)
    {
        if (!string.IsNullOrWhiteSpace(providerCode) &&
            TryGet(providerCode, out var provider) &&
            provider is not null)
        {
            return provider;
        }

        throw new MobileMoneyException(
            "provider_not_found",
            $"Mobile Money provider '{providerCode}' is not registered.");
    }
}