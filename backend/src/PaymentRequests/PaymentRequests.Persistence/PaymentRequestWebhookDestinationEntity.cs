namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestWebhookDestinationEntity
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SubscribedEvents { get; set; } = string.Empty;
    public int RetryMaxAttempts { get; set; }
    public long RetryBaseDelayMilliseconds { get; set; }
    public long RetryMaxDelayMilliseconds { get; set; }
}
