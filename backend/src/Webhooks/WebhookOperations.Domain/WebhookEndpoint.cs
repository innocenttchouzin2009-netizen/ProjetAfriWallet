using AfriWallet.Merchants.Registry.Domain.Merchants;

namespace AfriWallet.Webhooks.Operations.Domain;

public sealed class WebhookEndpoint
{
    private readonly HashSet<WebhookEventSubscription> _subscriptions;

    private WebhookEndpoint(
        WebhookEndpointId id,
        MerchantId ownerMerchantId,
        Uri endpointUri,
        WebhookSigningSecretReference signingSecretReference,
        IEnumerable<WebhookEventSubscription> subscriptions,
        WebhookDeliveryConfiguration deliveryConfiguration,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OwnerMerchantId = ownerMerchantId;
        EndpointUri = endpointUri;
        SigningSecretReference = signingSecretReference;
        DeliveryConfiguration = deliveryConfiguration;
        _subscriptions = new HashSet<WebhookEventSubscription>(subscriptions);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        EnabledAtUtc = createdAtUtc;
        IsEnabled = true;
    }

    public WebhookEndpointId Id { get; }
    public MerchantId OwnerMerchantId { get; }
    public Uri EndpointUri { get; private set; }
    public WebhookSigningSecretReference SigningSecretReference { get; private set; }
    public WebhookDeliveryConfiguration DeliveryConfiguration { get; private set; }
    public IReadOnlySet<WebhookEventSubscription> Subscriptions => _subscriptions;
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? EnabledAtUtc { get; private set; }
    public DateTimeOffset? DisabledAtUtc { get; private set; }
    public DateTimeOffset? SecretRotatedAtUtc { get; private set; }

    public static WebhookEndpoint Register(
        MerchantId ownerMerchantId,
        Uri endpointUri,
        WebhookSigningSecretReference signingSecretReference,
        IEnumerable<WebhookEventSubscription> subscriptions,
        WebhookDeliveryConfiguration deliveryConfiguration,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(endpointUri);
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(deliveryConfiguration);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        ValidateEndpointUri(endpointUri);

        var normalizedSubscriptions = subscriptions.Distinct().ToArray();
        if (normalizedSubscriptions.Length == 0)
            throw new ArgumentException("At least one webhook event subscription is required.", nameof(subscriptions));
        if (normalizedSubscriptions.Length > 64)
            throw new ArgumentException("A webhook endpoint can subscribe to at most 64 event types.", nameof(subscriptions));

        return new WebhookEndpoint(
            WebhookEndpointId.New(),
            ownerMerchantId,
            endpointUri,
            signingSecretReference,
            normalizedSubscriptions,
            deliveryConfiguration,
            createdAtUtc);
    }

    public void Enable(DateTimeOffset atUtc)
    {
        EnsureMutationTime(atUtc);
        if (IsEnabled)
            return;

        IsEnabled = true;
        EnabledAtUtc = atUtc;
        DisabledAtUtc = null;
        UpdatedAtUtc = atUtc;
    }

    public void Disable(DateTimeOffset atUtc)
    {
        EnsureMutationTime(atUtc);
        if (!IsEnabled)
            return;

        IsEnabled = false;
        DisabledAtUtc = atUtc;
        UpdatedAtUtc = atUtc;
    }

    public void RotateSigningSecret(
        WebhookSigningSecretReference newSecretReference,
        DateTimeOffset atUtc)
    {
        EnsureMutationTime(atUtc);
        if (newSecretReference == SigningSecretReference)
            throw new InvalidOperationException("Webhook signing secret rotation requires a new secret reference.");

        SigningSecretReference = newSecretReference;
        SecretRotatedAtUtc = atUtc;
        UpdatedAtUtc = atUtc;
    }

    public void ReplaceSubscriptions(
        IEnumerable<WebhookEventSubscription> subscriptions,
        DateTimeOffset atUtc)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        EnsureMutationTime(atUtc);

        var normalized = subscriptions.Distinct().ToArray();
        if (normalized.Length == 0)
            throw new ArgumentException("At least one webhook event subscription is required.", nameof(subscriptions));
        if (normalized.Length > 64)
            throw new ArgumentException("A webhook endpoint can subscribe to at most 64 event types.", nameof(subscriptions));

        _subscriptions.Clear();
        foreach (var subscription in normalized)
            _subscriptions.Add(subscription);

        UpdatedAtUtc = atUtc;
    }

    public void UpdateDeliveryConfiguration(
        WebhookDeliveryConfiguration deliveryConfiguration,
        DateTimeOffset atUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveryConfiguration);
        EnsureMutationTime(atUtc);
        DeliveryConfiguration = deliveryConfiguration;
        UpdatedAtUtc = atUtc;
    }

    private void EnsureMutationTime(DateTimeOffset atUtc)
    {
        EnsureUtc(atUtc, nameof(atUtc));
        if (atUtc < UpdatedAtUtc)
            throw new ArgumentException("Webhook endpoint mutation timestamp cannot move backwards.", nameof(atUtc));
    }

    private static void ValidateEndpointUri(Uri endpointUri)
    {
        if (!endpointUri.IsAbsoluteUri || endpointUri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Webhook endpoint must be an absolute HTTPS URI.", nameof(endpointUri));
        if (!string.IsNullOrEmpty(endpointUri.UserInfo) || !string.IsNullOrEmpty(endpointUri.Fragment))
            throw new ArgumentException("Webhook endpoint cannot contain user info or a fragment.", nameof(endpointUri));
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
