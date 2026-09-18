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

public sealed record PaymentRequestWebhookVerificationRequest(
    ReadOnlyMemory<byte> Payload,
    PaymentRequestWebhookHeaders Headers);

public enum PaymentRequestWebhookVerificationStatus
{
    Verified = 1,
    InvalidRequest = 2,
    TimestampOutsideTolerance = 3,
    UnknownOrInactiveKey = 4,
    InvalidSignature = 5,
    ReplayDetected = 6
}

public sealed record PaymentRequestWebhookVerificationResult(
    PaymentRequestWebhookVerificationStatus Status,
    string Code)
{
    public bool Succeeded => Status == PaymentRequestWebhookVerificationStatus.Verified;

    public static PaymentRequestWebhookVerificationResult Verified() =>
        new(PaymentRequestWebhookVerificationStatus.Verified, "WEBHOOK_VERIFIED");

    public static PaymentRequestWebhookVerificationResult InvalidRequest() =>
        new(PaymentRequestWebhookVerificationStatus.InvalidRequest, "WEBHOOK_INVALID_REQUEST");

    public static PaymentRequestWebhookVerificationResult TimestampOutsideTolerance() =>
        new(PaymentRequestWebhookVerificationStatus.TimestampOutsideTolerance, "WEBHOOK_TIMESTAMP_OUTSIDE_TOLERANCE");

    public static PaymentRequestWebhookVerificationResult UnknownOrInactiveKey() =>
        new(PaymentRequestWebhookVerificationStatus.UnknownOrInactiveKey, "WEBHOOK_KEY_UNAVAILABLE");

    public static PaymentRequestWebhookVerificationResult InvalidSignature() =>
        new(PaymentRequestWebhookVerificationStatus.InvalidSignature, "WEBHOOK_INVALID_SIGNATURE");

    public static PaymentRequestWebhookVerificationResult ReplayDetected() =>
        new(PaymentRequestWebhookVerificationStatus.ReplayDetected, "WEBHOOK_REPLAY_DETECTED");
}
