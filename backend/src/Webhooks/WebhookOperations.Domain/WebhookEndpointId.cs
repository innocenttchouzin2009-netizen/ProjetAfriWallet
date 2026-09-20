namespace AfriWallet.Webhooks.Operations.Domain;

public readonly record struct WebhookEndpointId
{
    public Guid Value { get; }

    private WebhookEndpointId(Guid value) => Value = value;

    public static WebhookEndpointId New() => new(Guid.NewGuid());

    public static WebhookEndpointId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Webhook endpoint id cannot be empty.", nameof(value));

        return new WebhookEndpointId(value);
    }

    public override string ToString() => Value.ToString();
}
