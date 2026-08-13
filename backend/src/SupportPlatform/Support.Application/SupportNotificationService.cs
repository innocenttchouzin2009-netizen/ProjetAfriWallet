using Notification.Application;
using Notification.Contracts;
using Support.Domain;

namespace Support.Application;

public sealed class SupportNotificationService
{
    private readonly NotificationService _notificationService;
    private readonly string _templateKey = "SUPPORT_CASE_UPDATE";
    public int ExternalNotificationsSent { get; private set; }
    public int InternalAlertsSent { get; private set; }

    public SupportNotificationService()
    {
        _notificationService = new NotificationService(
            new TemplateEngine(),
            new DeliveryDispatcher(new RetryService(), new EventPublisher()),
            new PreferenceService(),
            new EventPublisher());

        var template = _notificationService.CreateTemplate(new CreateTemplateRequest
        {
            Key = _templateKey,
            Version = 1,
            Localizations = new Dictionary<string, TemplateVariantRequest>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new() { Subject = "Support case {{reference}}", Body = "Status update for case {{reference}}: {{event}}." }
            }
        });

        _notificationService.PublishTemplate(template.TemplateId, new PublishTemplateRequest { PublishedBy = "support-system" });
    }

    public bool SendCaseNotification(SupportCase supportCase, string eventName)
    {
        if (string.IsNullOrWhiteSpace(supportCase.RequesterAwidId))
        {
            return false;
        }

        var response = _notificationService.CreateNotification(new CreateNotificationRequest
        {
            TemplateKey = _templateKey,
            Type = "SYSTEM",
            Priority = "NORMAL",
            Recipient = new NotificationRecipientRequest
            {
                Awid = supportCase.RequesterAwidId,
                Email = "customer-notification@afriwallet.local"
            },
            Channels = new List<string> { "IN_APP" },
            Parameters = new Dictionary<string, string>
            {
                ["reference"] = supportCase.CaseReference,
                ["event"] = eventName
            }
        });

        var delivered = response.Status == "DELIVERED";
        if (delivered)
        {
            ExternalNotificationsSent += 1;
        }

        return delivered;
    }

    public bool SendInternalSlaAlert(SupportCase supportCase, string eventName)
    {
        var response = _notificationService.CreateNotification(new CreateNotificationRequest
        {
            TemplateKey = _templateKey,
            Type = "RISK",
            Priority = "CRITICAL",
            Recipient = new NotificationRecipientRequest
            {
                Awid = supportCase.AssignedAgentId ?? "support-ops",
                Email = "support-ops@afriwallet.local"
            },
            Channels = new List<string> { "EMAIL" },
            Parameters = new Dictionary<string, string>
            {
                ["reference"] = supportCase.CaseReference,
                ["event"] = eventName
            }
        });

        var delivered = response.Status == "DELIVERED";
        if (delivered)
        {
            InternalAlertsSent += 1;
        }

        return delivered;
    }
}
