namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestEventOutboxEntity
{
    public Guid EventId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime EnqueuedAtUtc { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public int Status { get; set; }
    public int AttemptCount { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string? LastError { get; set; }
}
