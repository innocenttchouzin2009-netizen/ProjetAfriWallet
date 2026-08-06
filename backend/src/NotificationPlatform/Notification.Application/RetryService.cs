using Notification.Domain;

namespace Notification.Application;

public sealed class RetryService
{
    public IReadOnlyList<DeliveryAttempt> Execute(NotificationChannel channel, bool simulateTransientFailure)
    {
        var attempts = new List<DeliveryAttempt>();
        if (simulateTransientFailure && channel == NotificationChannel.Webhook)
        {
            attempts.Add(new DeliveryAttempt
            {
                AttemptNumber = 1,
                Channel = channel,
                Status = DeliveryStatus.Failed,
                ErrorMessage = "Transient provider timeout",
                Provider = ResolveProvider(channel),
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = 120
            });

            attempts.Add(new DeliveryAttempt
            {
                AttemptNumber = 2,
                Channel = channel,
                Status = DeliveryStatus.Delivered,
                Provider = ResolveProvider(channel),
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = 95
            });

            return attempts;
        }

        attempts.Add(new DeliveryAttempt
        {
            AttemptNumber = 1,
            Channel = channel,
            Status = DeliveryStatus.Delivered,
            Provider = ResolveProvider(channel),
            CompletedAt = DateTimeOffset.UtcNow,
            DurationMs = 40
        });

        return attempts;
    }

    private static string ResolveProvider(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => "smtp-primary",
            NotificationChannel.Sms => "sms-gateway",
            NotificationChannel.Push => "push-broker",
            NotificationChannel.InApp => "in-app-feed",
            NotificationChannel.Webhook => "webhook-delivery",
            _ => "unknown"
        };
    }
}
