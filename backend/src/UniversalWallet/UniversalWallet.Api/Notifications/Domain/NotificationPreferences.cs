namespace UniversalWallet.Api.Notifications.Domain;

public sealed class NotificationPreferences
{
    public Guid UserAwid { get; init; }
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool InAppEnabled { get; set; } = true;
    public bool MarketingEnabled { get; set; } = false;
    public bool SecurityAlertsEnabled { get; set; } = true;
    public bool PaymentAlertsEnabled { get; set; } = true;
    public string Language { get; set; } = "fr";
}
