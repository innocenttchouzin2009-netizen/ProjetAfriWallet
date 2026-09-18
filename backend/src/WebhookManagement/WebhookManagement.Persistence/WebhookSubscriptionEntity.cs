namespace AfriWallet.Webhooks.Persistence;

public sealed class WebhookSubscriptionEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string EventTypesJson { get; set; } = "[]";
    public string SigningKeyReference { get; set; } = string.Empty;
    public int Status { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
}
