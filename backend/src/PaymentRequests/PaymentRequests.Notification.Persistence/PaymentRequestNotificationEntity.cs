namespace AfriWallet.PaymentRequests.Notification.Persistence;

public sealed class PaymentRequestNotificationEntity
{
    public Guid Id { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public int Kind { get; set; }
    public int Audience { get; set; }
    public Guid RecipientOwnerId { get; set; }
    public Guid RequesterWalletId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public int RequestStatus { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Guid? TransferId { get; set; }
    public int ReadStatus { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}
