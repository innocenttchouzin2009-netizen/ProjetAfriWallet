namespace AfriWallet.PartnerWebhooks.Domain;

public readonly record struct PartnerWebhookSubscriptionId
{
    public Guid Value { get; }

    private PartnerWebhookSubscriptionId(Guid value) => Value = value;

    public static PartnerWebhookSubscriptionId New() => new(Guid.NewGuid());

    public static PartnerWebhookSubscriptionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(value));
        }

        return new PartnerWebhookSubscriptionId(value);
    }

    public override string ToString() => Value.ToString("D");
}
