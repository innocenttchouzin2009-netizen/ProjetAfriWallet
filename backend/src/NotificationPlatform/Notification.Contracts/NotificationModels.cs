using Notification.Domain;

namespace Notification.Contracts;

public sealed class CreateNotificationRequest
{
    public string TemplateKey { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public NotificationRecipientRequest Recipient { get; set; } = new();
    public List<string> Channels { get; set; } = new();
    public string? Locale { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool SimulateTransientFailure { get; set; }
}

public sealed class NotificationRecipientRequest
{
    public string Awid { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DeviceToken { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class NotificationPreferenceRequest
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
}

public sealed class CreateTemplateRequest
{
    public string Key { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Dictionary<string, TemplateVariantRequest> Localizations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TemplateVariantRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class PublishTemplateRequest
{
    public string PublishedBy { get; set; } = "system";
}

public sealed class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public NotificationRecipientDto Recipient { get; set; } = new();
    public List<string> RequestedChannels { get; set; } = new();
    public List<string> EffectiveChannels { get; set; } = new();
    public List<DeliveryAttemptDto> Attempts { get; set; } = new();
    public List<string> AuditEvents { get; set; } = new();
    public NotificationTelemetry Telemetry { get; set; } = new();
}

public sealed class NotificationRecipientDto
{
    public string Awid { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DeviceToken { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class DeliveryAttemptDto
{
    public int AttemptNumber { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}

public sealed class NotificationTelemetry
{
    public Dictionary<string, long> Metrics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, long> ChannelCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class NotificationPreferenceResponse
{
    public string Awid { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public bool MarketingOptIn { get; set; }
}

public sealed class NotificationTemplateResponse
{
    public Guid TemplateId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool Published { get; set; }
    public Dictionary<string, TemplateVariantDto> Localizations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> AuditEvents { get; set; } = new();
}

public sealed class TemplateVariantDto
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
