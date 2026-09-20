using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public interface IActivityEventPublisher
{
    Task<ActivityEventPublicationResult> PublishAsync(
        ActivityEvent activityEvent,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed record ActivityEventPublicationResult(
    Guid EventId,
    IReadOnlyList<NotificationDelivery> Deliveries);

public sealed class PreferenceAwareActivityEventPublisher(
    INotificationPreferenceRepository preferenceRepository,
    INotificationChannelPolicyProvider policyProvider)
    : IActivityEventPublisher
{
    public async Task<ActivityEventPublicationResult> PublishAsync(
        ActivityEvent activityEvent,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activityEvent);
        if (publishedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Publication timestamp must be UTC.", nameof(publishedAtUtc));
        if (publishedAtUtc < activityEvent.OccurredAtUtc)
            throw new ArgumentException("Publication timestamp cannot precede the activity event.", nameof(publishedAtUtc));

        cancellationToken.ThrowIfCancellationRequested();

        var policies = policyProvider.List();
        if (policies.Count == 0)
            return new ActivityEventPublicationResult(activityEvent.EventId, Array.Empty<NotificationDelivery>());

        var duplicateChannel = policies
            .GroupBy(policy => policy.Channel)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateChannel is not null)
            throw new InvalidOperationException($"Duplicate notification policy for channel '{duplicateChannel.Key}'.");

        var deliveries = new List<NotificationDelivery>();

        foreach (var policy in policies.OrderBy(policy => policy.Channel))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var enabled = policy.DefaultEnabled;
            if (policy.UserConfigurable)
            {
                var preference = await preferenceRepository.GetAsync(
                    activityEvent.UserId,
                    policy.Channel,
                    cancellationToken);

                if (preference is not null)
                    enabled = preference.IsEnabled;
            }

            if (!enabled)
                continue;

            deliveries.Add(NotificationDelivery.Create(activityEvent, policy.Channel, publishedAtUtc));
        }

        return new ActivityEventPublicationResult(activityEvent.EventId, deliveries);
    }
}
