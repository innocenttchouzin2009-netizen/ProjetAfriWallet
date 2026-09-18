namespace AfriWallet.Webhooks.Domain;

public sealed class WebhookSubscription
{
    private const int MaxEventTypes = 32;
    private const int MaxSigningKeyReferenceLength = 256;

    private WebhookSubscription(
        WebhookSubscriptionId id,
        Guid ownerId,
        Uri endpoint,
        IReadOnlyList<WebhookEventType> eventTypes,
        string signingKeyReference,
        WebhookSubscriptionStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        Endpoint = endpoint;
        EventTypes = eventTypes;
        SigningKeyReference = signingKeyReference;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public WebhookSubscriptionId Id { get; }
    public Guid OwnerId { get; }
    public Uri Endpoint { get; private set; }
    public IReadOnlyList<WebhookEventType> EventTypes { get; private set; }
    public string SigningKeyReference { get; private set; }
    public WebhookSubscriptionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static WebhookSubscription Create(
        Guid ownerId,
        Uri endpoint,
        IEnumerable<WebhookEventType> eventTypes,
        string signingKeyReference,
        DateTimeOffset createdAtUtc)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Webhook owner id cannot be empty.", nameof(ownerId));

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        var normalizedEndpoint = ValidateEndpoint(endpoint);
        var normalizedEventTypes = NormalizeEventTypes(eventTypes);
        var normalizedKeyReference = ValidateSigningKeyReference(signingKeyReference);

        return new WebhookSubscription(
            WebhookSubscriptionId.New(),
            ownerId,
            normalizedEndpoint,
            normalizedEventTypes,
            normalizedKeyReference,
            WebhookSubscriptionStatus.Active,
            createdAtUtc,
            createdAtUtc);
    }

    public static WebhookSubscription Restore(
        WebhookSubscriptionId id,
        Guid ownerId,
        Uri endpoint,
        IEnumerable<WebhookEventType> eventTypes,
        string signingKeyReference,
        WebhookSubscriptionStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Webhook owner id cannot be empty.", nameof(ownerId));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported webhook subscription status.");

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc)
            throw new ArgumentException("Webhook updated timestamp cannot precede creation.", nameof(updatedAtUtc));

        return new WebhookSubscription(
            id,
            ownerId,
            ValidateEndpoint(endpoint),
            NormalizeEventTypes(eventTypes),
            ValidateSigningKeyReference(signingKeyReference),
            status,
            createdAtUtc,
            updatedAtUtc);
    }

    public void ChangeEndpoint(Uri endpoint, DateTimeOffset changedAtUtc)
    {
        EnsureTransitionTime(changedAtUtc, nameof(changedAtUtc));
        Endpoint = ValidateEndpoint(endpoint);
        UpdatedAtUtc = changedAtUtc;
    }

    public void ReplaceEventTypes(IEnumerable<WebhookEventType> eventTypes, DateTimeOffset changedAtUtc)
    {
        EnsureTransitionTime(changedAtUtc, nameof(changedAtUtc));
        EventTypes = NormalizeEventTypes(eventTypes);
        UpdatedAtUtc = changedAtUtc;
    }

    public void ChangeSigningKeyReference(string signingKeyReference, DateTimeOffset changedAtUtc)
    {
        EnsureTransitionTime(changedAtUtc, nameof(changedAtUtc));
        SigningKeyReference = ValidateSigningKeyReference(signingKeyReference);
        UpdatedAtUtc = changedAtUtc;
    }

    public void Disable(DateTimeOffset disabledAtUtc)
    {
        EnsureTransitionTime(disabledAtUtc, nameof(disabledAtUtc));
        if (Status == WebhookSubscriptionStatus.Disabled)
            return;

        Status = WebhookSubscriptionStatus.Disabled;
        UpdatedAtUtc = disabledAtUtc;
    }

    public void Enable(DateTimeOffset enabledAtUtc)
    {
        EnsureTransitionTime(enabledAtUtc, nameof(enabledAtUtc));
        if (Status == WebhookSubscriptionStatus.Active)
            return;

        Status = WebhookSubscriptionStatus.Active;
        UpdatedAtUtc = enabledAtUtc;
    }

    private void EnsureTransitionTime(DateTimeOffset timestamp, string parameterName)
    {
        EnsureUtc(timestamp, parameterName);
        if (timestamp < UpdatedAtUtc)
            throw new ArgumentException("Webhook transition timestamp cannot move backwards.", parameterName);
    }

    private static Uri ValidateEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(endpoint.Host))
            throw new ArgumentException("Webhook endpoint must be an absolute HTTPS URI.", nameof(endpoint));

        if (!string.IsNullOrEmpty(endpoint.UserInfo))
            throw new ArgumentException("Webhook endpoint cannot contain user credentials.", nameof(endpoint));

        if (!string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("Webhook endpoint cannot contain a fragment.", nameof(endpoint));

        return endpoint;
    }

    private static IReadOnlyList<WebhookEventType> NormalizeEventTypes(IEnumerable<WebhookEventType> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        var normalized = eventTypes
            .Select(eventType => eventType ?? throw new ArgumentException("Webhook event type cannot be null.", nameof(eventTypes)))
            .GroupBy(eventType => eventType.Value, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(eventType => eventType.Value, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
            throw new ArgumentException("At least one webhook event type is required.", nameof(eventTypes));
        if (normalized.Length > MaxEventTypes)
            throw new ArgumentException($"A webhook subscription cannot contain more than {MaxEventTypes} event types.", nameof(eventTypes));

        return normalized;
    }

    private static string ValidateSigningKeyReference(string signingKeyReference)
    {
        if (string.IsNullOrWhiteSpace(signingKeyReference))
            throw new ArgumentException("Webhook signing key reference is required.", nameof(signingKeyReference));

        var normalized = signingKeyReference.Trim();
        if (normalized.Length > MaxSigningKeyReferenceLength)
            throw new ArgumentException(
                $"Webhook signing key reference cannot exceed {MaxSigningKeyReferenceLength} characters.",
                nameof(signingKeyReference));

        if (normalized.Any(char.IsWhiteSpace))
            throw new ArgumentException("Webhook signing key reference cannot contain whitespace.", nameof(signingKeyReference));

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Webhook timestamp must be UTC.", parameterName);
    }
}
