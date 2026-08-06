using Notification.Contracts;
using Notification.Domain;

namespace Notification.Application;

public sealed class NotificationService
{
    private readonly List<Notification.Domain.Notification> _notifications = new();
    private readonly List<NotificationTemplate> _templates = new();
    private readonly TemplateEngine _templateEngine;
    private readonly DeliveryDispatcher _deliveryDispatcher;
    private readonly PreferenceService _preferenceService;
    private readonly EventPublisher _eventPublisher;

    public NotificationService(
        TemplateEngine templateEngine,
        DeliveryDispatcher deliveryDispatcher,
        PreferenceService preferenceService,
        EventPublisher eventPublisher)
    {
        _templateEngine = templateEngine;
        _deliveryDispatcher = deliveryDispatcher;
        _preferenceService = preferenceService;
        _eventPublisher = eventPublisher;
    }

    public NotificationResponse CreateNotification(CreateNotificationRequest request)
    {
        var template = _templates
            .Where(x => string.Equals(x.Key, request.TemplateKey, StringComparison.OrdinalIgnoreCase) && x.Published)
            .OrderByDescending(x => x.Version)
            .First();

        var type = Enum.Parse<NotificationType>(request.Type, true);
        var priority = string.IsNullOrWhiteSpace(request.Priority)
            ? NotificationPriority.Normal
            : Enum.Parse<NotificationPriority>(request.Priority, true);
        var recipient = new NotificationRecipient
        {
            Awid = request.Recipient.Awid,
            Email = request.Recipient.Email,
            PhoneNumber = request.Recipient.PhoneNumber,
            DeviceToken = request.Recipient.DeviceToken,
            WebhookUrl = request.Recipient.WebhookUrl
        };

        var preference = _preferenceService.GetPreferenceEntity(recipient.Awid);
        var preferredLocale = string.IsNullOrWhiteSpace(request.Locale) ? preference.Language : request.Locale!;
        var rendered = _templateEngine.Render(template, preferredLocale, request.Parameters);
        var requestedChannels = request.Channels.Select(ParseChannel).ToList();
        var effectiveChannels = requestedChannels
            .Where(x => _preferenceService.IsChannelEnabled(preference, type, priority, x))
            .ToList();

        var notification = new Notification.Domain.Notification
        {
            TemplateKey = request.TemplateKey,
            Type = type,
            Priority = priority,
            Recipient = recipient,
            RequestedChannels = requestedChannels,
            EffectiveChannels = effectiveChannels,
            Locale = rendered.Locale,
            Subject = rendered.Subject,
            Body = rendered.Body,
            Status = effectiveChannels.Count == 0 ? DeliveryStatus.Cancelled : DeliveryStatus.Pending
        };

        _eventPublisher.Publish(notification, NotificationEvent.NotificationCreated);
        if (effectiveChannels.Count == 0)
        {
            _eventPublisher.Publish(notification, NotificationEvent.NotificationCancelled);
        }
        else
        {
            _deliveryDispatcher.Dispatch(notification, request.SimulateTransientFailure);
        }

        _notifications.Add(notification);
        return Map(notification);
    }

    public NotificationResponse GetNotification(Guid notificationId)
    {
        return Map(_notifications.Single(x => x.NotificationId == notificationId));
    }

    public NotificationPreferenceResponse GetPreferences(string awid)
    {
        return _preferenceService.GetPreferences(awid);
    }

    public NotificationPreferenceResponse UpdatePreferences(NotificationPreferenceRequest request)
    {
        var response = _preferenceService.UpdatePreferences(request);
        return response;
    }

    public IReadOnlyList<NotificationTemplateResponse> ListTemplates()
    {
        return _templates
            .OrderBy(x => x.Key)
            .ThenByDescending(x => x.Version)
            .Select(MapTemplate)
            .ToList();
    }

    public NotificationTemplateResponse CreateTemplate(CreateTemplateRequest request)
    {
        var template = new NotificationTemplate
        {
            Key = request.Key,
            Version = request.Version,
            Localizations = request.Localizations.ToDictionary(
                x => x.Key,
                x => new TemplateVariant { Subject = x.Value.Subject, Body = x.Value.Body },
                StringComparer.OrdinalIgnoreCase)
        };
        _templates.Add(template);
        return MapTemplate(template);
    }

    public NotificationTemplateResponse PublishTemplate(Guid templateId, PublishTemplateRequest request)
    {
        var template = _templates.Single(x => x.TemplateId == templateId);
        template.Published = true;
        template.PublishedAt = DateTimeOffset.UtcNow;
        _eventPublisher.Publish(template, NotificationEvent.TemplatePublished);
        return MapTemplate(template);
    }

    private static NotificationResponse Map(Notification.Domain.Notification notification)
    {
        var deliveredCount = notification.Attempts.Count(x => x.Status == DeliveryStatus.Delivered);
        var failedCount = notification.Attempts.Count(x => x.Status == DeliveryStatus.Failed);
        var retriedCount = notification.AuditEvents.Count(x => x == NotificationEvent.NotificationRetried);
        var avgDuration = notification.Attempts.Count == 0 ? 0 : (long)notification.Attempts.Average(x => x.DurationMs);
        var channelCounts = notification.Attempts
            .GroupBy(x => x.Channel.ToString().ToUpperInvariant())
            .ToDictionary(x => x.Key, x => (long)x.Count(), StringComparer.OrdinalIgnoreCase);

        return new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            TemplateKey = notification.TemplateKey,
            Type = notification.Type.ToString().ToUpperInvariant(),
            Priority = notification.Priority.ToString().ToUpperInvariant(),
            Locale = notification.Locale,
            Subject = notification.Subject,
            Body = notification.Body,
            Status = notification.Status.ToString().ToUpperInvariant(),
            Recipient = new NotificationRecipientDto
            {
                Awid = notification.Recipient.Awid,
                Email = notification.Recipient.Email,
                PhoneNumber = notification.Recipient.PhoneNumber,
                DeviceToken = notification.Recipient.DeviceToken,
                WebhookUrl = notification.Recipient.WebhookUrl
            },
            RequestedChannels = notification.RequestedChannels.Select(FormatChannel).ToList(),
            EffectiveChannels = notification.EffectiveChannels.Select(FormatChannel).ToList(),
            Attempts = notification.Attempts.Select(x => new DeliveryAttemptDto
            {
                AttemptNumber = x.AttemptNumber,
                Channel = FormatChannel(x.Channel),
                Status = x.Status.ToString().ToUpperInvariant(),
                ErrorMessage = x.ErrorMessage,
                DurationMs = x.DurationMs
            }).ToList(),
            AuditEvents = notification.AuditEvents.ToList(),
            Telemetry = new NotificationTelemetry
            {
                Metrics = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                {
                    ["afw_notifications_created_total"] = 1,
                    ["afw_notifications_sent_total"] = deliveredCount,
                    ["afw_notifications_failed_total"] = failedCount,
                    ["afw_notification_delivery_duration_ms"] = avgDuration,
                    ["afw_notification_retry_total"] = retriedCount,
                    ["afw_notification_channel_total"] = notification.EffectiveChannels.Count
                },
                ChannelCounts = channelCounts
            }
        };
    }

    private static NotificationTemplateResponse MapTemplate(NotificationTemplate template)
    {
        return new NotificationTemplateResponse
        {
            TemplateId = template.TemplateId,
            Key = template.Key,
            Version = template.Version,
            Published = template.Published,
            Localizations = template.Localizations.ToDictionary(
                x => x.Key,
                x => new TemplateVariantDto { Subject = x.Value.Subject, Body = x.Value.Body },
                StringComparer.OrdinalIgnoreCase),
            AuditEvents = template.AuditEvents.ToList()
        };
    }

    private static NotificationChannel ParseChannel(string value)
    {
        var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.Parse<NotificationChannel>(normalized, true);
    }

    private static string FormatChannel(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.InApp => "IN_APP",
            _ => channel.ToString().ToUpperInvariant()
        };
    }
}
