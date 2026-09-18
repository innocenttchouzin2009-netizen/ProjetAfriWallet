namespace AfriWallet.PartnerWebhooks.Domain;

public readonly record struct WebhookEventType
{
    public string Value { get; }

    private WebhookEventType(string value) => Value = value;

    public static WebhookEventType From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook event type is required.", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 128)
        {
            throw new ArgumentException("Webhook event type cannot exceed 128 characters.", nameof(value));
        }

        if (normalized.Any(char.IsWhiteSpace) ||
            normalized.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')))
        {
            throw new ArgumentException("Webhook event type contains unsupported characters.", nameof(value));
        }

        return new WebhookEventType(normalized);
    }

    public override string ToString() => Value;
}
