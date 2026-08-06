namespace Notification.Domain;

public sealed class NotificationPreference
{
    public string Awid { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public bool MarketingOptIn { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
