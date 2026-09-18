namespace AfriWallet.Webhooks.Domain;

public sealed record WebhookEventType
{
    private const int MaxLength = 128;

    private WebhookEventType(string value) => Value = value;

    public string Value { get; }

    public static WebhookEventType Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Webhook event type is required.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Webhook event type cannot exceed {MaxLength} characters.", nameof(value));

        if (normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not ('.' or '-' or '_')))
        {
            throw new ArgumentException(
                "Webhook event type may contain only ASCII letters, digits, '.', '-' or '_'.",
                nameof(value));
        }

        return new WebhookEventType(normalized);
    }

    public override string ToString() => Value;
}
