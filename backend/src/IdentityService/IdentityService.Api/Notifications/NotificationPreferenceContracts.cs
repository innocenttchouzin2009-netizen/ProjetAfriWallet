namespace IdentityService.Api.Notifications;

public sealed record UpdateNotificationPreferenceRequest(bool IsEnabled);

public sealed record NotificationPreferenceResponse(
    string Channel,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record NotificationPreferenceUpdateResponse(
    string Status,
    NotificationPreferenceResponse Preference);

public sealed record NotificationPreferenceErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class NotificationPreferenceErrorCode
{
    public const string Unauthorized = "NOTIFICATION_PREFERENCE_UNAUTHORIZED";
    public const string ValidationError = "NOTIFICATION_PREFERENCE_VALIDATION_ERROR";
    public const string Conflict = "NOTIFICATION_PREFERENCE_CONFLICT";
}
