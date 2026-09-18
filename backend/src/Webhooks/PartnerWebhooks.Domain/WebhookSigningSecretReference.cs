namespace AfriWallet.PartnerWebhooks.Domain;

public readonly record struct WebhookSigningSecretReference
{
    public string Value { get; }

    private WebhookSigningSecretReference(string value) => Value = value;

    public static WebhookSigningSecretReference From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Webhook signing secret reference is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > 256)
        {
            throw new ArgumentException("Webhook signing secret reference cannot exceed 256 characters.", nameof(value));
        }

        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Webhook signing secret reference cannot contain whitespace.", nameof(value));
        }

        return new WebhookSigningSecretReference(normalized);
    }

    public override string ToString() => Value;
}
