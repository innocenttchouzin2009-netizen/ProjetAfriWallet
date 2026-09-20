namespace AfriWallet.PaymentRequests.Webhooks;

public static class PaymentRequestWebhookHeaderNames
{
    public const string EventId = "X-AfWal-Webhook-Id";
    public const string Timestamp = "X-AfWal-Webhook-Timestamp";
    public const string KeyId = "X-AfWal-Webhook-Key-Id";
    public const string Signature = "X-AfWal-Webhook-Signature";
}

public sealed record PaymentRequestWebhookHeaders(
    Guid EventId,
    long TimestampUnixSeconds,
    string KeyId,
    string Signature);
