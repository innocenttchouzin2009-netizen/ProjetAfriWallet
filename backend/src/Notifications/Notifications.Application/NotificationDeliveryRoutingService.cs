using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public enum NotificationDeliveryDecisionSource
{
    ChannelPolicy = 1,
    UserPreference = 2
}

public sealed record NotificationDeliveryDecision(
    Guid UserId,
    NotificationChannel Channel,
    bool IsEnabled,
    NotificationDeliveryDecisionSource Source);

public sealed class NotificationDeliveryRoutingService(
    INotificationPreferenceRepository preferenceRepository,
    INotificationChannelPolicyProvider policyProvider)
{
    public async Task<NotificationDeliveryDecision> EvaluateAsync(
        Guid userId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel), "Notification channel is invalid.");

        cancellationToken.ThrowIfCancellationRequested();
        var policy = policyProvider.Get(channel);

        if (!policy.UserConfigurable)
        {
            return new NotificationDeliveryDecision(
                userId,
                channel,
                policy.DefaultEnabled,
                NotificationDeliveryDecisionSource.ChannelPolicy);
        }

        var preference = await preferenceRepository.GetAsync(userId, channel, cancellationToken);
        return preference is null
            ? new NotificationDeliveryDecision(
                userId,
                channel,
                policy.DefaultEnabled,
                NotificationDeliveryDecisionSource.ChannelPolicy)
            : new NotificationDeliveryDecision(
                userId,
                channel,
                preference.IsEnabled,
                NotificationDeliveryDecisionSource.UserPreference);
    }
}
