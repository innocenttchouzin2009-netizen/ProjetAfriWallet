using Notification.Application;
using Notification.Contracts;

var service = new NotificationService(
    new TemplateEngine(),
    new DeliveryDispatcher(new RetryService(), new EventPublisher()),
    new PreferenceService(),
    new EventPublisher());

var template = service.CreateTemplate(new CreateTemplateRequest
{
    Key = "PAYMENT_RECEIVED",
    Version = 1,
    Localizations = new Dictionary<string, TemplateVariantRequest>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new() { Subject = "Payment received", Body = "Hello {{name}}, payment {{amount}} received." },
        ["fr"] = new() { Subject = "Paiement recu", Body = "Bonjour {{name}}, paiement {{amount}} recu." },
        ["de"] = new() { Subject = "Zahlung eingegangen", Body = "Hallo {{name}}, Zahlung {{amount}} erhalten." },
        ["sw"] = new() { Subject = "Malipo yamepokelewa", Body = "Habari {{name}}, malipo {{amount}} yamepokelewa." }
    }
});
service.PublishTemplate(template.TemplateId, new PublishTemplateRequest { PublishedBy = "ops@afriwallet" });

service.UpdatePreferences(new NotificationPreferenceRequest
{
    Awid = "aw-001",
    EmailEnabled = true,
    SmsEnabled = true,
    PushEnabled = true,
    InAppEnabled = true,
    Language = "en",
    Timezone = "Africa/Abidjan",
    MarketingOptIn = false
});

var emailNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "PAYMENT",
    Priority = "NORMAL",
    Recipient = new NotificationRecipientRequest { Awid = "aw-001", Email = "customer@example.com" },
    Channels = new List<string> { "EMAIL" },
    Parameters = new Dictionary<string, string> { ["name"] = "Awa", ["amount"] = "10 000 XOF" }
});
if (emailNotification.Status != "DELIVERED" || !emailNotification.EffectiveChannels.Contains("EMAIL")) throw new Exception("email notification failed");

var smsNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "PAYMENT",
    Priority = "HIGH",
    Recipient = new NotificationRecipientRequest { Awid = "aw-001", PhoneNumber = "+2250700000000" },
    Channels = new List<string> { "SMS" },
    Parameters = new Dictionary<string, string> { ["name"] = "Awa", ["amount"] = "10 000 XOF" }
});
if (smsNotification.Status != "DELIVERED" || !smsNotification.EffectiveChannels.Contains("SMS")) throw new Exception("sms notification failed");

var pushNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "PAYMENT",
    Priority = "NORMAL",
    Recipient = new NotificationRecipientRequest { Awid = "aw-001", DeviceToken = "push-token-001" },
    Channels = new List<string> { "PUSH" },
    Parameters = new Dictionary<string, string> { ["name"] = "Awa", ["amount"] = "10 000 XOF" }
});
if (pushNotification.Status != "DELIVERED" || !pushNotification.EffectiveChannels.Contains("PUSH")) throw new Exception("push notification failed");

var inAppNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "PAYMENT",
    Priority = "NORMAL",
    Recipient = new NotificationRecipientRequest { Awid = "aw-001" },
    Channels = new List<string> { "IN_APP" },
    Parameters = new Dictionary<string, string> { ["name"] = "Awa", ["amount"] = "10 000 XOF" }
});
if (inAppNotification.Status != "DELIVERED" || !inAppNotification.EffectiveChannels.Contains("IN_APP")) throw new Exception("in-app notification failed");

if (!emailNotification.Body.Contains("Awa") || !emailNotification.Body.Contains("10 000 XOF")) throw new Exception("template rendering failed");

service.UpdatePreferences(new NotificationPreferenceRequest
{
    Awid = "aw-002",
    EmailEnabled = true,
    SmsEnabled = true,
    PushEnabled = true,
    InAppEnabled = true,
    Language = "sw",
    Timezone = "Africa/Nairobi",
    MarketingOptIn = true
});

var localizedNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "PAYMENT",
    Priority = "NORMAL",
    Recipient = new NotificationRecipientRequest { Awid = "aw-002", Email = "sw@example.com" },
    Channels = new List<string> { "EMAIL" },
    Parameters = new Dictionary<string, string> { ["name"] = "Juma", ["amount"] = "25 000 KES" }
});
if (localizedNotification.Locale != "sw" || !localizedNotification.Body.Contains("Habari Juma")) throw new Exception("localized template failed");

service.UpdatePreferences(new NotificationPreferenceRequest
{
    Awid = "aw-003",
    EmailEnabled = false,
    SmsEnabled = false,
    PushEnabled = false,
    InAppEnabled = false,
    Language = "en",
    Timezone = "UTC",
    MarketingOptIn = false
});

var preferenceNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "MARKETING",
    Priority = "LOW",
    Recipient = new NotificationRecipientRequest { Awid = "aw-003", Email = "optout@example.com" },
    Channels = new List<string> { "EMAIL" },
    Parameters = new Dictionary<string, string> { ["name"] = "OptOut", ["amount"] = "0" }
});
if (preferenceNotification.Status != "CANCELLED" || preferenceNotification.EffectiveChannels.Count != 0) throw new Exception("user preferences failed");

var webhookNotification = service.CreateNotification(new CreateNotificationRequest
{
    TemplateKey = "PAYMENT_RECEIVED",
    Type = "SYSTEM",
    Priority = "HIGH",
    Recipient = new NotificationRecipientRequest { Awid = "aw-004", WebhookUrl = "https://hooks.afriwallet.local/test" },
    Channels = new List<string> { "WEBHOOK" },
    Parameters = new Dictionary<string, string> { ["name"] = "Ops", ["amount"] = "N/A" },
    SimulateTransientFailure = true
});
if (webhookNotification.Attempts.Count != 2 || webhookNotification.Attempts[0].Status != "FAILED" || webhookNotification.Attempts[1].Status != "DELIVERED") throw new Exception("retry delivery failed");
if (webhookNotification.Attempts.Count < 2 || webhookNotification.Attempts[^1].DurationMs <= 0) throw new Exception("delivery tracking failed");
if (!webhookNotification.AuditEvents.Contains("NOTIFICATION_CREATED") || !webhookNotification.AuditEvents.Contains("NOTIFICATION_DISPATCHED") || !webhookNotification.AuditEvents.Contains("NOTIFICATION_DELIVERED") || !webhookNotification.AuditEvents.Contains("NOTIFICATION_RETRIED")) throw new Exception("audit generation failed");
if (!webhookNotification.Telemetry.Metrics.ContainsKey("afw_notifications_created_total") || !webhookNotification.Telemetry.Metrics.ContainsKey("afw_notification_retry_total") || webhookNotification.Telemetry.ChannelCounts.Keys.Any(x => x.Contains("@") || x.Contains("+"))) throw new Exception("telemetry generation failed");

Console.WriteLine("email notification ................. PASS");
Console.WriteLine("sms notification ................... PASS");
Console.WriteLine("push notification .................. PASS");
Console.WriteLine("in-app notification ................ PASS");
Console.WriteLine("template rendering ................. PASS");
Console.WriteLine("localized template ................. PASS");
Console.WriteLine("user preferences ................... PASS");
Console.WriteLine("retry delivery ..................... PASS");
Console.WriteLine("delivery tracking .................. PASS");
Console.WriteLine("audit generation ................... PASS");
Console.WriteLine("telemetry generation ............... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0012.1 notification platform scenarios passed.");
