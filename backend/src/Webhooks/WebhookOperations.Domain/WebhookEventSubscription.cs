using System.Text.RegularExpressions;

namespace AfriWallet.Webhooks.Operations.Domain;

public readonly record struct WebhookEventSubscription
{
    private static readonly Regex Allowed = new(
        "^[a-z0-9]+(?:[._-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public WebhookEventSubscription(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Webhook event subscription is required.", nameof(eventType));

        var normalized = eventType.Trim().ToLowerInvariant();
        if (normalized.Length > 128 || !Allowed.IsMatch(normalized))
            throw new ArgumentException("Webhook event subscription has an invalid format.", nameof(eventType));

        EventType = normalized;
    }

    public string EventType { get; }

    public override string ToString() => EventType;
}
