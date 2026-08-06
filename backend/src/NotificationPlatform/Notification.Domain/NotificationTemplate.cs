namespace Notification.Domain;

public sealed class NotificationTemplate
{
    public Guid TemplateId { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool Published { get; set; }
    public Dictionary<string, TemplateVariant> Localizations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
    public List<string> AuditEvents { get; set; } = new();
}

public sealed class TemplateVariant
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
