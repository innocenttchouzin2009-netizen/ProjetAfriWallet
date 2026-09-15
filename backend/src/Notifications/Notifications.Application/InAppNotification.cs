using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.Notifications.Application;

public enum NotificationChannel
{
    InApp = 1
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

public interface INotificationDeliveryPort
{
    Task DeliverAsync(
        InAppNotification notification,
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
