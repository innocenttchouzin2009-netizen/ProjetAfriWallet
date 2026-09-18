namespace AfriWallet.PaymentRequests.Webhooks.Infrastructure;

public sealed record PaymentRequestWebhookTransportOptions(
    Uri Endpoint,
    string SignatureHeaderName = "X-AfWal-Signature",
    string IdempotencyHeaderName = "Idempotency-Key",
    string EventTypeHeaderName = "X-AfWal-Event",
    string MessageIdHeaderName = "X-AfWal-Message-Id")
{
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Endpoint);

        if (!Endpoint.IsAbsoluteUri ||
            (Endpoint.Scheme != Uri.UriSchemeHttps && Endpoint.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Webhook endpoint must be an absolute HTTP or HTTPS URI.", nameof(Endpoint));
        }

        ValidateHeaderName(SignatureHeaderName, nameof(SignatureHeaderName));
        ValidateHeaderName(IdempotencyHeaderName, nameof(IdempotencyHeaderName));
        ValidateHeaderName(EventTypeHeaderName, nameof(EventTypeHeaderName));
        ValidateHeaderName(MessageIdHeaderName, nameof(MessageIdHeaderName));
    }

    private static void ValidateHeaderName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook header name cannot be empty.", parameterName);
        }
    }
}
