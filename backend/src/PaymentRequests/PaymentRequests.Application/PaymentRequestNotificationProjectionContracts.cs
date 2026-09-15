using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestNotificationProjectionSource(
    Guid SourceEventId,
    PaymentRequestId PaymentRequestId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    PaymentRequestStatus Status,
    WalletId RequesterWalletId,
    Currency Currency,
    long AmountMinor,
    DateTimeOffset? ExpiresAtUtc,
    WalletId? AcceptedPayerWalletId,
    Guid? TransferId);

public sealed record PaymentRequestNotificationRecipient(
    Guid OwnerId,
    PaymentRequestNotificationAudience Audience);

public sealed record PaymentRequestNotificationProjectionKey(
    Guid SourceEventId,
    Guid RecipientOwnerId,
    PaymentRequestNotificationAudience Audience);

public interface IPaymentRequestNotificationAudienceResolver
{
    Task<IReadOnlyList<PaymentRequestNotificationRecipient>> ResolveAsync(
        PaymentRequestNotificationProjectionSource source,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestNotificationProjectionStore
{
    Task<bool> TryAddAsync(
        PaymentRequestNotification notification,
        CancellationToken cancellationToken = default);
}

public static class PaymentRequestNotificationEventTypes
{
    public const string Created = "payment-request.created";
    public const string Accepted = "payment-request.accepted";
    public const string Declined = "payment-request.declined";
    public const string Cancelled = "payment-request.cancelled";
    public const string Expired = "payment-request.expired";
    public const string Paid = "payment-request.paid";

    public static bool TryGetKind(string eventType, out PaymentRequestNotificationKind kind)
    {
        kind = eventType switch
        {
            Created => PaymentRequestNotificationKind.Created,
            Accepted => PaymentRequestNotificationKind.Accepted,
            Declined => PaymentRequestNotificationKind.Declined,
            Cancelled => PaymentRequestNotificationKind.Cancelled,
            Expired => PaymentRequestNotificationKind.Expired,
            Paid => PaymentRequestNotificationKind.Paid,
            _ => default
        };

        return eventType is Created or Accepted or Declined or Cancelled or Expired or Paid;
    }
}
