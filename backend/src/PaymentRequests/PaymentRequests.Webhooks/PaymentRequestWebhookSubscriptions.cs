using System.Text.RegularExpressions;

namespace AfriWallet.PaymentRequests.Webhooks;

public enum PaymentRequestWebhookSubscriptionStatus
{
    Active = 1,
    Disabled = 2
}

public sealed class PaymentRequestWebhookSubscription
{
    private static readonly Regex SecretReferencePattern =
        new("^[A-Z0-9_]{3,128}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private PaymentRequestWebhookSubscription(
        Guid id,
        string integrationId,
        Guid? merchantId,
        Uri endpoint,
        PaymentRequestWebhookSubscriptionStatus status,
        string keyId,
        string secretReference,
        IReadOnlyList<string> eventTypes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        IntegrationId = integrationId;
        MerchantId = merchantId;
        Endpoint = endpoint;
        Status = status;
        KeyId = keyId;
        SecretReference = secretReference;
        EventTypes = eventTypes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }
    public string IntegrationId { get; }
    public Guid? MerchantId { get; }
    public Uri Endpoint { get; }
    public PaymentRequestWebhookSubscriptionStatus Status { get; private set; }
    public string KeyId { get; }
    public string SecretReference { get; }
    public IReadOnlyList<string> EventTypes { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PaymentRequestWebhookSubscription Create(
        string integrationId,
        Guid? merchantId,
        Uri endpoint,
        string keyId,
        string secretReference,
        IEnumerable<string> eventTypes,
        DateTimeOffset createdAtUtc) =>
        Restore(Guid.NewGuid(), integrationId, merchantId, endpoint,
            PaymentRequestWebhookSubscriptionStatus.Active, keyId, secretReference,
            eventTypes, createdAtUtc, createdAtUtc);

    public static PaymentRequestWebhookSubscription Restore(
        Guid id,
        string integrationId,
        Guid? merchantId,
        Uri endpoint,
        PaymentRequestWebhookSubscriptionStatus status,
        string keyId,
        string secretReference,
        IEnumerable<string> eventTypes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(id));
        integrationId = NormalizeIntegrationId(integrationId);
        ValidateEndpoint(endpoint);
        ValidateKeyId(keyId);
        ValidateSecretReference(secretReference);
        var normalizedEvents = NormalizeEventTypes(eventTypes);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc) throw new ArgumentException("Updated timestamp cannot precede creation.", nameof(updatedAtUtc));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));

        return new PaymentRequestWebhookSubscription(
            id, integrationId, merchantId, endpoint, status, keyId,
            secretReference, normalizedEvents, createdAtUtc, updatedAtUtc);
    }

    public bool SubscribesTo(string eventType) =>
        EventTypes.Contains(NormalizeEventType(eventType), StringComparer.Ordinal);

    public void Disable(DateTimeOffset atUtc)
    {
        EnsureUtc(atUtc, nameof(atUtc));
        if (atUtc < UpdatedAtUtc) throw new ArgumentException("Subscription timestamp cannot move backwards.", nameof(atUtc));
        Status = PaymentRequestWebhookSubscriptionStatus.Disabled;
        UpdatedAtUtc = atUtc;
    }

    public void Enable(DateTimeOffset atUtc)
    {
        EnsureUtc(atUtc, nameof(atUtc));
        if (atUtc < UpdatedAtUtc) throw new ArgumentException("Subscription timestamp cannot move backwards.", nameof(atUtc));
        Status = PaymentRequestWebhookSubscriptionStatus.Active;
        UpdatedAtUtc = atUtc;
    }

    public static string NormalizeEventType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Webhook event type is required.", nameof(value));
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 128 || normalized.Any(char.IsWhiteSpace))
            throw new ArgumentException("Webhook event type is invalid.", nameof(value));
        return normalized;
    }

    private static IReadOnlyList<string> NormalizeEventTypes(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var normalized = values.Select(NormalizeEventType)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0) throw new ArgumentException("At least one webhook event type is required.", nameof(values));
        if (normalized.Length > 64) throw new ArgumentException("A webhook subscription cannot contain more than 64 event types.", nameof(values));
        return normalized;
    }

    private static string NormalizeIntegrationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Integration id is required.", nameof(value));
        var normalized = value.Trim();
        if (normalized.Length > 128 || normalized.Any(char.IsWhiteSpace))
            throw new ArgumentException("Integration id is invalid.", nameof(value));
        return normalized;
    }

    private static void ValidateEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Webhook endpoint must be an absolute HTTP or HTTPS URI.", nameof(endpoint));
        if (!string.IsNullOrEmpty(endpoint.UserInfo))
            throw new ArgumentException("Webhook endpoint must not contain user information.", nameof(endpoint));
    }

    private static void ValidateKeyId(string keyId)
    {
        if (!RotatingPaymentRequestWebhookSecretProvider.IsValidKeyId(keyId))
            throw new ArgumentException("Webhook key id is invalid.", nameof(keyId));
    }

    private static void ValidateSecretReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !SecretReferencePattern.IsMatch(value))
            throw new ArgumentException("Webhook secret reference must be an environment-style key name.", nameof(value));
    }

    private static void EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", name);
    }
}

public interface IPaymentRequestWebhookSubscriptionRegistry
{
    Task<PaymentRequestWebhookSubscription?> GetAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListActiveForEventAsync(string eventType, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default);
}

public interface IPaymentRequestWebhookSigningSecretResolver
{
    Task<string> ResolveAsync(string secretReference, CancellationToken cancellationToken = default);
}

public sealed class EnvironmentPaymentRequestWebhookSigningSecretResolver
    : IPaymentRequestWebhookSigningSecretResolver
{
    public Task<string> ResolveAsync(string secretReference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var secret = Environment.GetEnvironmentVariable(secretReference);
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("Webhook signing secret is unavailable or invalid.");
        return Task.FromResult(secret);
    }
}
