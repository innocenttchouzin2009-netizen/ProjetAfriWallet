using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Domain;

public enum PaymentRequestLifecycleEventKind
{
    Created = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4,
    Expired = 5,
    Paid = 6
}

public sealed record PaymentRequestLifecycleEvent(
    Guid EventId,
    PaymentRequestId PaymentRequestId,
    PaymentRequestLifecycleEventKind Kind,
    DateTimeOffset OccurredAtUtc,
    PaymentRequestStatus Status,
    WalletId RequesterWalletId,
    string CurrencyCode,
    long AmountMinor,
    DateTimeOffset? ExpiresAtUtc,
    WalletId? AcceptedPayerWalletId,
    Guid? TransferId);
