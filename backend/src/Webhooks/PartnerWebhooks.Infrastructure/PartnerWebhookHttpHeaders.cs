namespace AfriWallet.PartnerWebhooks.Infrastructure;

public static class PartnerWebhookHttpHeaders
{
    public const string EventId = "X-AfWal-Event-Id";
    public const string EventType = "X-AfWal-Event-Type";
    public const string Timestamp = "X-AfWal-Timestamp";
    public const string Signature = "X-AfWal-Signature";
    public const string SignatureAlgorithm = "X-AfWal-Signature-Algorithm";
    public const string SubscriptionId = "X-AfWal-Subscription-Id";
    public const string PartnerId = "X-AfWal-Partner-Id";
}
