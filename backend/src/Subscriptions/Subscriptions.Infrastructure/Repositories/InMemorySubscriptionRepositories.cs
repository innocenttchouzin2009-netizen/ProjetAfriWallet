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
