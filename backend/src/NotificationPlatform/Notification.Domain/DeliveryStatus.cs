namespace Notification.Domain;

public enum DeliveryStatus
{
    Pending,
    Dispatched,
    Delivered,
    Failed,
    Retried,
    Cancelled
}
