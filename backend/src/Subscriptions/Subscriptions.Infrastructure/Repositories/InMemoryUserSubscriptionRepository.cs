using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;

namespace Subscriptions.Infrastructure.Repositories;

public sealed class InMemoryUserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly Dictionary<string, UserSubscription> _subscriptionsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string UserId, string ProviderId), string> _index = new();

    public UserSubscription Add(UserSubscription subscription)
    {
        if (string.IsNullOrWhiteSpace(subscription.SubscriptionId))
        {
            subscription.SubscriptionId = Guid.NewGuid().ToString("N");
        }

        _subscriptionsById[subscription.SubscriptionId] = subscription;
        _index[(subscription.UserId, subscription.ProviderId)] = subscription.SubscriptionId;
        return subscription;
    }

    public UserSubscription Update(UserSubscription subscription)
    {
        _subscriptionsById[subscription.SubscriptionId] = subscription;
        _index[(subscription.UserId, subscription.ProviderId)] = subscription.SubscriptionId;
        return subscription;
    }

    public UserSubscription? GetById(string subscriptionId)
    {
        return _subscriptionsById.TryGetValue(subscriptionId, out var subscription) ? subscription : null;
    }

    public UserSubscription? FindByUserAndProvider(string userId, string providerId)
    {
        if (_index.TryGetValue((userId, providerId), out var subscriptionId))
        {
            return _subscriptionsById[subscriptionId];
        }

        return null;
    }
}
