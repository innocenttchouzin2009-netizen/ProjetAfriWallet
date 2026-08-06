namespace Notification.Domain;

public sealed class NotificationRecipient
{
    public string Awid { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DeviceToken { get; set; }
    public string? WebhookUrl { get; set; }
}
