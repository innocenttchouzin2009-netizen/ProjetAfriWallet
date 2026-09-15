namespace AfriWallet.Notifications.Application;

public enum NotificationChannel
{
    InApp = 1
}

public sealed record NotificationDelivery(
    Guid NotificationId,
    Guid RecipientUserId,
    NotificationChannel Channel,
    string Type,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc,
    string? DataJson = null);

public enum NotificationDeliveryFailureKind
{
    Transient = 1,
    Permanent = 2
}

public sealed class NotificationDeliveryException : Exception
{
    public NotificationDeliveryException(
        NotificationDeliveryFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public NotificationDeliveryFailureKind FailureKind { get; }
}

public interface INotificationDeliveryPort
{
    Task DeliverAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default);
}
