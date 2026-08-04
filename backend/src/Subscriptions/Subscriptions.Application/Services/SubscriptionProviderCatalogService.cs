using Subscriptions.Domain.Models;

namespace Subscriptions.Application.Services;

public sealed class SubscriptionProviderCatalogService
{
    private readonly ISubscriptionProviderRepository _providerRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public SubscriptionProviderCatalogService(
        ISubscriptionProviderRepository providerRepository,
        ISubscriptionPlanRepository planRepository)
    {
        _providerRepository = providerRepository;
        _planRepository = planRepository;
    }

    public IReadOnlyList<SubscriptionProvider> ListProviders(string? country = null, string? currency = null, string? q = null, int page = 1, int pageSize = 20)
    {
        var providers = _providerRepository.ListAll();

        if (!string.IsNullOrWhiteSpace(q))
        {
            providers = providers.Where(p =>
                p.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Code.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            providers = providers.Where(p => p.SupportedCountries.Contains(country, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            providers = providers.Where(p => p.SupportedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        return providers
            .OrderBy(p => p.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public SubscriptionProvider? GetProvider(string providerId)
    {
        return _providerRepository.GetById(providerId);
    }

    public IReadOnlyList<SubscriptionPlan> GetPlans(string providerId)
    {
        return _planRepository.ListByProvider(providerId);
    }
}

public interface ISubscriptionProviderRepository
{
    SubscriptionProvider Create(SubscriptionProvider provider);
    SubscriptionProvider? GetById(string providerId);
    IReadOnlyList<SubscriptionProvider> ListAll();
}

public interface ISubscriptionPlanRepository
{
    SubscriptionPlan Create(SubscriptionPlan plan);
    IReadOnlyList<SubscriptionPlan> ListByProvider(string providerId);
}
