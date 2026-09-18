namespace AfriWallet.Webhooks.Domain;

public readonly record struct WebhookSubscriptionId
{
    public Guid Value { get; }

    private WebhookSubscriptionId(Guid value) => Value = value;

    public static WebhookSubscriptionId New() => new(Guid.NewGuid());

    public static WebhookSubscriptionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(value));

        return new WebhookSubscriptionId(value);
    }

    public override string ToString() => Value.ToString("D");
}
