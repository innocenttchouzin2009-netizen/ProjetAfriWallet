using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public sealed record NotificationPreferenceSnapshot(
    Guid Id,
    Guid UserId,
    NotificationChannel Channel,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateNotificationPreferenceCommand(
    Guid UserId,
    NotificationChannel Channel,
    bool IsEnabled,
    DateTimeOffset UpdatedAtUtc);

public enum UpdateNotificationPreferenceStatus
{
    Created = 1,
    Updated = 2,
    Unchanged = 3
}

public sealed record UpdateNotificationPreferenceResult(
    UpdateNotificationPreferenceStatus Status,
    NotificationPreferenceSnapshot Preference);

internal static class NotificationPreferenceMappings
{
    public static NotificationPreferenceSnapshot ToSnapshot(NotificationPreference preference) => new(
        preference.Id,
        preference.UserId,
        preference.Channel,
        preference.IsEnabled,
        preference.CreatedAtUtc,
        preference.UpdatedAtUtc);
}
