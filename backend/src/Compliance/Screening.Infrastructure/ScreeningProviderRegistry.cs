using AfriWallet.Compliance.Screening.Application.Abstractions;

namespace AfriWallet.Compliance.Screening.Infrastructure;

public sealed class ScreeningProviderRegistry : IScreeningProviderRegistry
{
    private readonly IReadOnlyCollection<IScreeningListProvider> _providers;

    public ScreeningProviderRegistry(IEnumerable<IScreeningListProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public IReadOnlyCollection<IScreeningListProvider> All() => _providers;
}