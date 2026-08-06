using Notification.Contracts;
using Notification.Domain;

namespace Notification.Application;

public sealed class PreferenceService
{
    private readonly List<NotificationPreference> _preferences = new();

    public NotificationPreferenceResponse GetPreferences(string awid)
    {
        return Map(GetOrCreate(awid));
    }

    public NotificationPreferenceResponse UpdatePreferences(NotificationPreferenceRequest request)
    {
        var preference = GetOrCreate(request.Awid);
        preference.EmailEnabled = request.EmailEnabled;
        preference.SmsEnabled = request.SmsEnabled;
        preference.PushEnabled = request.PushEnabled;
        preference.InAppEnabled = request.InAppEnabled;
        preference.Language = request.Language;
        preference.Timezone = request.Timezone;
        preference.QuietHoursStart = request.QuietHoursStart;
        preference.QuietHoursEnd = request.QuietHoursEnd;
        preference.MarketingOptIn = request.MarketingOptIn;
        preference.UpdatedAt = DateTimeOffset.UtcNow;
        return Map(preference);
    }

    public NotificationPreference GetPreferenceEntity(string awid) => GetOrCreate(awid);

    public bool IsChannelEnabled(NotificationPreference preference, NotificationType type, NotificationPriority priority, NotificationChannel channel)
    {
        var securityOverride = priority == NotificationPriority.Critical && type is NotificationType.Security or NotificationType.Risk or NotificationType.Compliance;
        if (securityOverride && channel is NotificationChannel.Email or NotificationChannel.Sms or NotificationChannel.Push or NotificationChannel.InApp)
        {
            return true;
        }

        if (type == NotificationType.Marketing && !preference.MarketingOptIn)
        {
            return false;
        }

        return channel switch
        {
            NotificationChannel.Email => preference.EmailEnabled,
            NotificationChannel.Sms => preference.SmsEnabled,
            NotificationChannel.Push => preference.PushEnabled,
            NotificationChannel.InApp => preference.InAppEnabled,
            NotificationChannel.Webhook => true,
            _ => false
        };
    }

    private NotificationPreference GetOrCreate(string awid)
    {
        var existing = _preferences.SingleOrDefault(x => string.Equals(x.Awid, awid, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var preference = new NotificationPreference { Awid = awid };
        _preferences.Add(preference);
        return preference;
    }

    private static NotificationPreferenceResponse Map(NotificationPreference preference)
    {
        return new NotificationPreferenceResponse
        {
            Awid = preference.Awid,
            EmailEnabled = preference.EmailEnabled,
            SmsEnabled = preference.SmsEnabled,
            PushEnabled = preference.PushEnabled,
            InAppEnabled = preference.InAppEnabled,
            Language = preference.Language,
            Timezone = preference.Timezone,
            QuietHoursStart = preference.QuietHoursStart,
            QuietHoursEnd = preference.QuietHoursEnd,
            MarketingOptIn = preference.MarketingOptIn
        };
    }
}
