namespace AfriWallet.PartnerWebhooks.Domain;

public sealed class PartnerWebhookSubscription
{
    private const int MaxEventTypes = 64;
    private IReadOnlyList<WebhookEventType> eventTypes;

    private PartnerWebhookSubscription(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        WebhookEndpoint endpoint,
        WebhookSigningSecretReference signingSecretReference,
        IReadOnlyList<WebhookEventType> eventTypes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        PartnerId = partnerId;
        Endpoint = endpoint;
        SigningSecretReference = signingSecretReference;
        this.eventTypes = eventTypes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Status = PartnerWebhookSubscriptionStatus.Active;
    }

    public PartnerWebhookSubscriptionId Id { get; }
    public PartnerId PartnerId { get; }
    public WebhookEndpoint Endpoint { get; private set; }
    public WebhookSigningSecretReference SigningSecretReference { get; private set; }
    public IReadOnlyList<WebhookEventType> EventTypes => eventTypes;
    public PartnerWebhookSubscriptionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PartnerWebhookSubscription Create(
        PartnerId partnerId,
        WebhookEndpoint endpoint,
        WebhookSigningSecretReference signingSecretReference,
        IEnumerable<WebhookEventType> eventTypes,
        DateTimeOffset createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new PartnerWebhookSubscription(
            PartnerWebhookSubscriptionId.New(),
            partnerId,
            endpoint,
            signingSecretReference,
            NormalizeEventTypes(eventTypes),
            createdAtUtc);
    }

    public void UpdateEndpoint(WebhookEndpoint endpoint, DateTimeOffset updatedAtUtc)
    {
        EnsureMutable(updatedAtUtc);
        Endpoint = endpoint;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceEventTypes(IEnumerable<WebhookEventType> newEventTypes, DateTimeOffset updatedAtUtc)
    {
        EnsureMutable(updatedAtUtc);
        eventTypes = NormalizeEventTypes(newEventTypes);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RotateSigningSecretReference(
        WebhookSigningSecretReference signingSecretReference,
        DateTimeOffset updatedAtUtc)
    {
        EnsureMutable(updatedAtUtc);
        SigningSecretReference = signingSecretReference;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Suspend(DateTimeOffset suspendedAtUtc)
    {
        EnsureTransitionTime(suspendedAtUtc);
        if (Status != PartnerWebhookSubscriptionStatus.Active)
        {
            throw new InvalidOperationException("Only an active webhook subscription can be suspended.");
        }

        Status = PartnerWebhookSubscriptionStatus.Suspended;
        UpdatedAtUtc = suspendedAtUtc;
    }

    public void Resume(DateTimeOffset resumedAtUtc)
    {
        EnsureTransitionTime(resumedAtUtc);
        if (Status != PartnerWebhookSubscriptionStatus.Suspended)
        {
            throw new InvalidOperationException("Only a suspended webhook subscription can be resumed.");
        }

        Status = PartnerWebhookSubscriptionStatus.Active;
        UpdatedAtUtc = resumedAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        EnsureTransitionTime(revokedAtUtc);
        if (Status == PartnerWebhookSubscriptionStatus.Revoked)
        {
            throw new InvalidOperationException("Webhook subscription is already revoked.");
        }

        Status = PartnerWebhookSubscriptionStatus.Revoked;
        UpdatedAtUtc = revokedAtUtc;
    }

    public bool Accepts(WebhookEventType eventType) =>
        Status == PartnerWebhookSubscriptionStatus.Active && eventTypes.Contains(eventType);

    private void EnsureMutable(DateTimeOffset updatedAtUtc)
    {
        EnsureTransitionTime(updatedAtUtc);
        if (Status == PartnerWebhookSubscriptionStatus.Revoked)
        {
            throw new InvalidOperationException("Revoked webhook subscriptions are immutable.");
        }
    }

    private void EnsureTransitionTime(DateTimeOffset value)
    {
        EnsureUtc(value, nameof(value));
        if (value < UpdatedAtUtc)
        {
            throw new ArgumentException("Webhook subscription timestamp cannot move backwards.", nameof(value));
        }
    }

    private static IReadOnlyList<WebhookEventType> NormalizeEventTypes(IEnumerable<WebhookEventType> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Distinct()
            .OrderBy(value => value.Value, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one webhook event type is required.", nameof(values));
        }

        if (normalized.Length > MaxEventTypes)
        {
            throw new ArgumentException($"A webhook subscription cannot contain more than {MaxEventTypes} event types.", nameof(values));
        }

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Webhook subscription timestamps must be UTC.", parameterName);
        }
    }
}
