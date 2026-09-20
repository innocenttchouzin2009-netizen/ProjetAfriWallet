namespace AfriWallet.PaymentRequests.Application;

public static class PaymentRequestWebhookHeaders
{
    public const string EventId = "X-AfWal-Event-Id";
    public const string EventType = "X-AfWal-Event-Type";
    public const string Timestamp = "X-AfWal-Timestamp";
    public const string Signature = "X-AfWal-Signature";
    public const string SignatureAlgorithm = "X-AfWal-Signature-Algorithm";\n    public const string SignatureKeyId = "X-AfWal-Signature-Key-Id";
}

public sealed record PaymentRequestWebhookSignatureInput(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset AttemptedAtUtc,
    string PayloadJson);

public sealed record PaymentRequestWebhookSignature(
    string Algorithm,
    string Value,
    string? KeyId = null);

public interface IPaymentRequestWebhookSigner
{
    Task<PaymentRequestWebhookSignature> SignAsync(
        PaymentRequestWebhookSignatureInput input,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestWebhookSignedRequest(
    Uri Endpoint,
    string HttpMethod,
    string ContentType,
    string PayloadJson,
    IReadOnlyDictionary<string, string> Headers);

public enum PaymentRequestWebhookDeliveryOutcome
{
    Delivered = 1,
    TransientFailure = 2,
    PermanentFailure = 3
}

public sealed record PaymentRequestWebhookTransportResult(
    PaymentRequestWebhookDeliveryOutcome Outcome,
    int? StatusCode = null,
    string? ProviderRequestId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static PaymentRequestWebhookTransportResult Delivered(
        int? statusCode = null,
        string? providerRequestId = null) =>
        new(PaymentRequestWebhookDeliveryOutcome.Delivered, statusCode, providerRequestId);

    public static PaymentRequestWebhookTransportResult TransientFailure(
        int? statusCode = null,
        string? errorCode = null,
        string? errorMessage = null) =>
        new(PaymentRequestWebhookDeliveryOutcome.TransientFailure, statusCode, null, errorCode, errorMessage);

    public static PaymentRequestWebhookTransportResult PermanentFailure(
        int? statusCode = null,
        string? errorCode = null,
        string? errorMessage = null) =>
        new(PaymentRequestWebhookDeliveryOutcome.PermanentFailure, statusCode, null, errorCode, errorMessage);
}

public interface IPaymentRequestWebhookTransport
{
    Task<PaymentRequestWebhookTransportResult> SendAsync(
        PaymentRequestWebhookSignedRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestWebhookDeliveryAttempt(
    Guid EventId,
    DateTimeOffset AttemptedAtUtc,
    PaymentRequestWebhookDeliveryOutcome Outcome,
    int? StatusCode,
    string? ProviderRequestId,
    string? ErrorCode,
    string? ErrorMessage);

public sealed class PaymentRequestWebhookDeliveryService(
    IPaymentRequestWebhookSigner signer,
    IPaymentRequestWebhookTransport transport)
{
    public async Task<PaymentRequestWebhookDeliveryAttempt> DeliverOnceAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        Uri endpoint,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        ArgumentNullException.ThrowIfNull(endpoint);

        cancellationToken.ThrowIfCancellationRequested();

        if (paymentRequestEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.PaymentRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestEvent));
        }

        if (string.IsNullOrWhiteSpace(paymentRequestEvent.EventType))
        {
            throw new ArgumentException("Event type is required.", nameof(paymentRequestEvent));
        }

        if (string.IsNullOrWhiteSpace(paymentRequestEvent.PayloadJson))
        {
            throw new ArgumentException("Webhook payload is required.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamp must be UTC.", nameof(paymentRequestEvent));
        }

        if (attemptedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Attempt timestamp must be UTC.", nameof(attemptedAtUtc));
        }

        if (attemptedAtUtc < paymentRequestEvent.OccurredAtUtc)
        {
            throw new ArgumentException("Attempt timestamp cannot be earlier than event timestamp.", nameof(attemptedAtUtc));
        }

        if (!endpoint.IsAbsoluteUri ||
            (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Webhook endpoint must be an absolute HTTP or HTTPS URI.", nameof(endpoint));
        }

        var signature = await signer.SignAsync(
            new PaymentRequestWebhookSignatureInput(
                paymentRequestEvent.EventId,
                paymentRequestEvent.EventType,
                paymentRequestEvent.OccurredAtUtc,
                attemptedAtUtc,
                paymentRequestEvent.PayloadJson),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(signature.Algorithm))
        {
            throw new InvalidOperationException("Webhook signature algorithm is required.");
        }

        if (string.IsNullOrWhiteSpace(signature.Value))
        {
            throw new InvalidOperationException("Webhook signature value is required.");
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PaymentRequestWebhookHeaders.EventId] = paymentRequestEvent.EventId.ToString(),
            [PaymentRequestWebhookHeaders.EventType] = paymentRequestEvent.EventType,
            [PaymentRequestWebhookHeaders.Timestamp] = attemptedAtUtc.ToString("O"),
            [PaymentRequestWebhookHeaders.SignatureAlgorithm] = signature.Algorithm,
            [PaymentRequestWebhookHeaders.Signature] = signature.Value
        };

        if (!string.IsNullOrWhiteSpace(signature.KeyId))
        {
            headers[PaymentRequestWebhookHeaders.SignatureKeyId] = signature.KeyId;
        }

        var result = await transport.SendAsync(
            new PaymentRequestWebhookSignedRequest(
                endpoint,
                "POST",
                "application/json",
                paymentRequestEvent.PayloadJson,
                headers),
            cancellationToken);

        return new PaymentRequestWebhookDeliveryAttempt(
            paymentRequestEvent.EventId,
            attemptedAtUtc,
            result.Outcome,
            result.StatusCode,
            result.ProviderRequestId,
            result.ErrorCode,
            result.ErrorMessage);
    }
}
