using Notification.Domain;

namespace Notification.Application;

public sealed class DeliveryDispatcher
{
    private readonly RetryService _retryService;
    private readonly EventPublisher _eventPublisher;

    public DeliveryDispatcher(RetryService retryService, EventPublisher eventPublisher)
    {
        _retryService = retryService;
        _eventPublisher = eventPublisher;
    }

    public void Dispatch(Notification.Domain.Notification notification, bool simulateTransientFailure)
    {
        foreach (var channel in notification.EffectiveChannels)
        {
            _eventPublisher.Publish(notification, NotificationEvent.NotificationDispatched);
            var attempts = _retryService.Execute(channel, simulateTransientFailure);
            foreach (var attempt in attempts)
            {
                notification.Attempts.Add(attempt);
                if (attempt.Status == DeliveryStatus.Failed)
                {
                    _eventPublisher.Publish(notification, NotificationEvent.NotificationFailed);
                    _eventPublisher.Publish(notification, NotificationEvent.NotificationRetried);
                }
            }
        }

        notification.Status = notification.Attempts.Any(x => x.Status == DeliveryStatus.Delivered)
            ? DeliveryStatus.Delivered
            : DeliveryStatus.Cancelled;
        notification.DeliveredAt = notification.Status == DeliveryStatus.Delivered ? DateTimeOffset.UtcNow : null;
        if (notification.Status == DeliveryStatus.Delivered)
        {
            _eventPublisher.Publish(notification, NotificationEvent.NotificationDelivered);
        }
        else
        {
            _eventPublisher.Publish(notification, NotificationEvent.NotificationCancelled);
        }
    }
}
