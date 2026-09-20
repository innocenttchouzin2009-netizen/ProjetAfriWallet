using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.Notifications.Application;

public enum NotificationChannel
{
    InApp = 1,
    Push = 2
}

public sealed record InAppNotification(
    Guid NotificationId,
    Guid SourceEventId,
    Guid RecipientUserId,
    Guid PaymentRequestId,
    string EventType,
    long AmountMinor,
    string CurrencyCode,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc,
    NotificationChannel Channel);

public sealed record InAppNotificationItem(
    Guid NotificationId,
    Guid SourceEventId,
    Guid RecipientUserId,
    Guid PaymentRequestId,
    string EventType,
    long AmountMinor,
    string CurrencyCode,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc,
    bool IsRead,
    DateTimeOffset? ReadAtUtc);

public interface INotificationDeliveryPort
{
    Task DeliverAsync(
        InAppNotification notification,
        CancellationToken cancellationToken = default);
}

public interface IInAppNotificationReader
{
    Task<IReadOnlyList<InAppNotificationItem>> ListByRecipientAsync(
        Guid recipientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<InAppNotificationItem?> GetAsync(
        Guid recipientUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestNotificationContext(
    Guid RecipientUserId,
    long AmountMinor,
    string CurrencyCode);

public interface IPaymentRequestNotificationRecipientResolver
{
    Task<PaymentRequestNotificationContext> ResolveAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default);
}
