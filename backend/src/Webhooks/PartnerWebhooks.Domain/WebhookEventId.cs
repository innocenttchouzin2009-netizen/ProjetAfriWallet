namespace AfriWallet.PartnerWebhooks.Domain;

public readonly record struct WebhookEventId
{
    public Guid Value { get; }

    private WebhookEventId(Guid value) => Value = value;

    public static WebhookEventId New() => new(Guid.NewGuid());

    public static WebhookEventId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Webhook event id cannot be empty.", nameof(value));
        }

        return new WebhookEventId(value);
    }

    public override string ToString() => Value.ToString();
}
