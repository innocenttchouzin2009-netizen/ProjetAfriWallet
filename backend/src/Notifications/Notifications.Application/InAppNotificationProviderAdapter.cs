namespace AfriWallet.Notifications.Application;

public sealed class InAppNotificationProviderAdapter(IInAppNotificationProvider provider)
    : INotificationDeliveryPort
{
    public async Task DeliverAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        if (notification.NotificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification id cannot be empty.", nameof(notification));
        }

        if (notification.RecipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Recipient user id cannot be empty.", nameof(notification));
        }

        if (notification.Channel != NotificationChannel.InApp)
        {
            throw new ArgumentException("In-app adapter only supports the InApp channel.", nameof(notification));
        }

        if (string.IsNullOrWhiteSpace(notification.Type) || notification.Type.Length > 128)
        {
            throw new ArgumentException("Notification type is required and must not exceed 128 characters.", nameof(notification));
        }

        if (string.IsNullOrWhiteSpace(notification.Title) || notification.Title.Length > 160)
        {
            throw new ArgumentException("Notification title is required and must not exceed 160 characters.", nameof(notification));
        }

        if (string.IsNullOrWhiteSpace(notification.Body) || notification.Body.Length > 4096)
        {
            throw new ArgumentException("Notification body is required and must not exceed 4096 characters.", nameof(notification));
        }

        if (notification.CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Notification timestamp must be UTC.", nameof(notification));
        }

        try
        {
            await provider.PublishAsync(
                new InAppNotificationMessage(
                    notification.NotificationId,
                    notification.RecipientUserId,
                    notification.Type.Trim(),
                    notification.Title.Trim(),
                    notification.Body,
                    notification.CreatedAtUtc,
                    notification.DataJson),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InAppNotificationProviderException exception)
        {
            var failureKind = exception.FailureKind switch
            {
                InAppNotificationProviderFailureKind.Transient => NotificationDeliveryFailureKind.Transient,
                InAppNotificationProviderFailureKind.Permanent => NotificationDeliveryFailureKind.Permanent,
                _ => throw new ArgumentOutOfRangeException(nameof(exception.FailureKind))
            };

            throw new NotificationDeliveryException(failureKind, exception.Message, exception);
        }
    }
}
