namespace Notification.Domain;

public static class NotificationEvent
{
    public const string NotificationCreated = "NOTIFICATION_CREATED";
    public const string NotificationDispatched = "NOTIFICATION_DISPATCHED";
    public const string NotificationDelivered = "NOTIFICATION_DELIVERED";
    public const string NotificationFailed = "NOTIFICATION_FAILED";
    public const string NotificationRetried = "NOTIFICATION_RETRIED";
    public const string NotificationCancelled = "NOTIFICATION_CANCELLED";
    public const string PreferenceUpdated = "PREFERENCE_UPDATED";
    public const string TemplatePublished = "TEMPLATE_PUBLISHED";
}
