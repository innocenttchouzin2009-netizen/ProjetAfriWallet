namespace AfriWallet.PartnerWebhooks.Domain;

public readonly record struct WebhookEndpoint
{
    public string Value { get; }

    private WebhookEndpoint(string value) => Value = value;

    public static WebhookEndpoint From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook endpoint is required.", nameof(value));
        }

        var candidate = value.Trim();
        if (candidate.Length > 2048)
        {
            throw new ArgumentException("Webhook endpoint cannot exceed 2048 characters.", nameof(value));
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("Webhook endpoint must be an absolute HTTPS URI.", nameof(value));
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Webhook endpoint must not contain user credentials.", nameof(value));
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Webhook endpoint must not contain a URI fragment.", nameof(value));
        }

        return new WebhookEndpoint(uri.AbsoluteUri);
    }

    public override string ToString() => Value;
}
