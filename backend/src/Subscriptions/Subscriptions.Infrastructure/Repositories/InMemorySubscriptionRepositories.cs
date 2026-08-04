using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;

namespace Subscriptions.Infrastructure.Repositories;

public sealed class InMemorySubscriptionProviderRepository : ISubscriptionProviderRepository
{
    private readonly Dictionary<string, SubscriptionProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public InMemorySubscriptionProviderRepository(IEnumerable<SubscriptionProvider>? seed = null)
    {
        if (seed is null) return;

        foreach (var provider in seed)
        {
            _providers[provider.ProviderId] = provider;
        }
    }

    public SubscriptionProvider Create(SubscriptionProvider provider)
    {
        if (string.IsNullOrWhiteSpace(provider.ProviderId))
        {
            provider.ProviderId = Guid.NewGuid().ToString("N");
        }

        _providers[provider.ProviderId] = provider;
        return provider;
    }

    public SubscriptionProvider? GetById(string providerId)
    {
        return _providers.TryGetValue(providerId, out var provider) ? provider : null;
    }

    public IReadOnlyList<SubscriptionProvider> ListAll()
    {
        return _providers.Values.OrderBy(p => p.DisplayName).ToList();
    }
}

public sealed class InMemorySubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly List<SubscriptionPlan> _plans = new();

    public InMemorySubscriptionPlanRepository(IEnumerable<SubscriptionPlan>? seed = null)
    {
        if (seed is not null)
        {
            _plans.AddRange(seed);
        }
    }

    public SubscriptionPlan Create(SubscriptionPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.PlanId))
        {
            plan.PlanId = Guid.NewGuid().ToString("N");
        }

        _plans.Add(plan);
        return plan;
    }

    public IReadOnlyList<SubscriptionPlan> ListByProvider(string providerId)
    {
        return _plans.Where(p => p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}

public sealed class InMemorySubscriptionCatalogOfferRepository
{
    private readonly List<SubscriptionCatalogOffer> _offers = new();

    public InMemorySubscriptionCatalogOfferRepository(IEnumerable<SubscriptionCatalogOffer>? seed = null)
    {
        if (seed is not null)
        {
            _offers.AddRange(seed);
        }
    }

    public SubscriptionCatalogOffer Create(SubscriptionCatalogOffer offer)
    {
        if (string.IsNullOrWhiteSpace(offer.OfferId))
        {
            offer.OfferId = Guid.NewGuid().ToString("N");
        }

        _offers.Add(offer);
        return offer;
    }

    public SubscriptionCatalogOffer? GetById(string offerId)
    {
        return _offers.FirstOrDefault(o => o.OfferId.Equals(offerId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SubscriptionCatalogOffer> List(
        string? country = null,
        string? currency = null,
        SubscriptionCategory? category = null,
        bool? featured = null,
        string? q = null,
        string? sort = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _offers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(o => o.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(o => o.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase));
        }

        if (category.HasValue)
        {
            query = query.Where(o => o.Category == category.Value);
        }

        if (featured.HasValue)
        {
            query = query.Where(o => o.IsFeatured == featured.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(o =>
                o.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.ProviderId.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        query = sort?.ToLowerInvariant() switch
        {
            "priceascending" => query.OrderBy(o => o.PriceMinor),
            "pricedescending" => query.OrderByDescending(o => o.PriceMinor),
            "newest" => query.OrderByDescending(o => o.CreatedAt),
            _ => query.OrderBy(o => o.Name)
        };

        return query
            .Where(o => o.IsAvailable)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
