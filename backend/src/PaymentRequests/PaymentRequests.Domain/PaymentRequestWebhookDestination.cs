namespace AfriWallet.PaymentRequests.Domain;

public readonly record struct PaymentRequestWebhookDestinationId
{
    public Guid Value { get; }

    private PaymentRequestWebhookDestinationId(Guid value) => Value = value;

    public static PaymentRequestWebhookDestinationId New() => new(Guid.NewGuid());

    public static PaymentRequestWebhookDestinationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Webhook destination id cannot be empty.", nameof(value));

        return new PaymentRequestWebhookDestinationId(value);
    }
}

public readonly record struct PaymentRequestWebhookRecipientId
{
    public Guid Value { get; }

    private PaymentRequestWebhookRecipientId(Guid value) => Value = value;

    public static PaymentRequestWebhookRecipientId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Webhook recipient id cannot be empty.", nameof(value));

        return new PaymentRequestWebhookRecipientId(value);
    }
}

public sealed record PaymentRequestWebhookRetryPolicy(
    int MaxAttempts,
    TimeSpan BaseDelay,
    TimeSpan MaxDelay)
{
    public static PaymentRequestWebhookRetryPolicy Default { get; } =
        new(5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(15));

    public void Validate()
    {
        if (MaxAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "Webhook retry max attempts must be positive.");
        if (BaseDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(BaseDelay), "Webhook retry base delay must be positive.");
        if (MaxDelay < BaseDelay)
            throw new ArgumentOutOfRangeException(nameof(MaxDelay), "Webhook retry max delay cannot be shorter than base delay.");
    }

    public TimeSpan DelayForAttempt(int attemptNumber)
    {
        Validate();
        if (attemptNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive.");

        var delay = BaseDelay;
        for (var current = 1; current < attemptNumber && delay < MaxDelay; current++)
        {
            if (delay.Ticks > MaxDelay.Ticks / 2)
                return MaxDelay;

            delay = TimeSpan.FromTicks(delay.Ticks * 2);
        }

        return delay > MaxDelay ? MaxDelay : delay;
    }
}

public sealed class PaymentRequestWebhookDestination
{
    private readonly HashSet<PaymentRequestLifecycleEventKind> subscribedEvents;

    private PaymentRequestWebhookDestination(
        PaymentRequestWebhookDestinationId id,
        PaymentRequestWebhookRecipientId recipientId,
        Uri endpoint,
        bool isActive,
        IEnumerable<PaymentRequestLifecycleEventKind> subscribedEvents,
        PaymentRequestWebhookRetryPolicy retryPolicy)
    {
        Id = id;
        RecipientId = recipientId;
        Endpoint = ValidateEndpoint(endpoint);
        this.subscribedEvents = NormalizeSubscriptions(subscribedEvents);
        RetryPolicy = ValidateRetryPolicy(retryPolicy);
        IsActive = isActive;
    }

    public PaymentRequestWebhookDestinationId Id { get; }
    public PaymentRequestWebhookRecipientId RecipientId { get; }
    public Uri Endpoint { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlySet<PaymentRequestLifecycleEventKind> SubscribedEvents => subscribedEvents;
    public PaymentRequestWebhookRetryPolicy RetryPolicy { get; private set; }

    public static PaymentRequestWebhookDestination Create(
        PaymentRequestWebhookRecipientId recipientId,
        Uri endpoint,
        IEnumerable<PaymentRequestLifecycleEventKind> subscribedEvents,
        PaymentRequestWebhookRetryPolicy? retryPolicy = null,
        bool isActive = true) =>
        new(
            PaymentRequestWebhookDestinationId.New(),
            recipientId,
            endpoint,
            isActive,
            subscribedEvents,
            retryPolicy ?? PaymentRequestWebhookRetryPolicy.Default);

    public static PaymentRequestWebhookDestination Restore(
        PaymentRequestWebhookDestinationId id,
        PaymentRequestWebhookRecipientId recipientId,
        Uri endpoint,
        bool isActive,
        IEnumerable<PaymentRequestLifecycleEventKind> subscribedEvents,
        PaymentRequestWebhookRetryPolicy retryPolicy) =>
        new(id, recipientId, endpoint, isActive, subscribedEvents, retryPolicy);

    public bool IsSubscribedTo(PaymentRequestLifecycleEventKind eventKind) =>
        IsActive && subscribedEvents.Contains(eventKind);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void ChangeEndpoint(Uri endpoint) => Endpoint = ValidateEndpoint(endpoint);

    public void ReplaceSubscriptions(IEnumerable<PaymentRequestLifecycleEventKind> eventKinds)
    {
        var normalized = NormalizeSubscriptions(eventKinds);
        subscribedEvents.Clear();
        subscribedEvents.UnionWith(normalized);
    }

    public void ChangeRetryPolicy(PaymentRequestWebhookRetryPolicy retryPolicy) =>
        RetryPolicy = ValidateRetryPolicy(retryPolicy);

    private static Uri ValidateEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri ||
            (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException(
                "Webhook destination endpoint must be an absolute HTTP or HTTPS URI.",
                nameof(endpoint));
        }

        if (!string.IsNullOrEmpty(endpoint.UserInfo))
            throw new ArgumentException("Webhook destination endpoint must not contain user information.", nameof(endpoint));

        if (!string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("Webhook destination endpoint must not contain a fragment.", nameof(endpoint));

        return endpoint;
    }

    private static HashSet<PaymentRequestLifecycleEventKind> NormalizeSubscriptions(
        IEnumerable<PaymentRequestLifecycleEventKind> eventKinds)
    {
        ArgumentNullException.ThrowIfNull(eventKinds);

        var values = eventKinds.ToHashSet();
        if (values.Count == 0)
            throw new ArgumentException("At least one payment request lifecycle event subscription is required.", nameof(eventKinds));

        if (values.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Webhook destination contains an unsupported lifecycle event subscription.", nameof(eventKinds));

        return values;
    }

    private static PaymentRequestWebhookRetryPolicy ValidateRetryPolicy(PaymentRequestWebhookRetryPolicy retryPolicy)
    {
        ArgumentNullException.ThrowIfNull(retryPolicy);
        retryPolicy.Validate();
        return retryPolicy;
    }
}
