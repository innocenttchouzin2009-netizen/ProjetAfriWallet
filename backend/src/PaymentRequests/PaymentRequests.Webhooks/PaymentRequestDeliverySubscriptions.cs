using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AfriWallet.P2P.Domain;

namespace AfriWallet.PaymentRequests.Webhooks;

public enum PaymentRequestDeliverySubscriptionStatus
{
    Active = 1,
    Disabled = 2
}

public sealed record PaymentRequestDeliveryRetryPolicy(int MaxAttempts, TimeSpan BaseRetryDelay)
{
    public static PaymentRequestDeliveryRetryPolicy Default { get; } =
        new(5, TimeSpan.FromSeconds(30));

    public void Validate()
    {
        if (MaxAttempts is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "Max attempts must be between 1 and 20.");
        if (BaseRetryDelay < TimeSpan.FromSeconds(1) || BaseRetryDelay > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(BaseRetryDelay), "Base retry delay must be between 1 second and 1 hour.");
    }
}

public sealed record PaymentRequestDeliveryRecipientBinding(string Kind, string Key)
{
    public static PaymentRequestDeliveryRecipientBinding From(RecipientReference recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        return recipient.Kind switch
        {
            RecipientReferenceKind.AfWalId => new("afwal-id", recipient.Value),
            RecipientReferenceKind.QrToken => new("qr-sha256", Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(recipient.Value))).ToLowerInvariant()),
            _ => throw new ArgumentOutOfRangeException(nameof(recipient.Kind))
        };
    }
}

public sealed class PaymentRequestDeliverySubscription
{
    private static readonly Regex SecretReferencePattern =
        new("^[A-Z0-9_]{3,128}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private PaymentRequestDeliverySubscription(
        Guid id, Guid ownerId, PaymentRequestDeliveryRecipientBinding recipient,
        Uri endpoint, PaymentRequestDeliverySubscriptionStatus status,
        string keyId, string secretReference, IReadOnlyList<string> eventTypes,
        PaymentRequestDeliveryRetryPolicy retryPolicy,
        DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        Id = id; OwnerId = ownerId; Recipient = recipient; Endpoint = endpoint; Status = status;
        KeyId = keyId; SecretReference = secretReference; EventTypes = eventTypes;
        RetryPolicy = retryPolicy; CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public PaymentRequestDeliveryRecipientBinding Recipient { get; }
    public Uri Endpoint { get; }
    public PaymentRequestDeliverySubscriptionStatus Status { get; private set; }
    public string KeyId { get; }
    public string SecretReference { get; }
    public IReadOnlyList<string> EventTypes { get; }
    public PaymentRequestDeliveryRetryPolicy RetryPolicy { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PaymentRequestDeliverySubscription Create(
        Guid ownerId, RecipientReference recipient, Uri endpoint, string keyId,
        string secretReference, IEnumerable<string> eventTypes,
        PaymentRequestDeliveryRetryPolicy retryPolicy, DateTimeOffset createdAtUtc) =>
        Restore(Guid.NewGuid(), ownerId, PaymentRequestDeliveryRecipientBinding.From(recipient),
            endpoint, PaymentRequestDeliverySubscriptionStatus.Active, keyId, secretReference,
            eventTypes, retryPolicy, createdAtUtc, createdAtUtc);

    public static PaymentRequestDeliverySubscription Restore(
        Guid id, Guid ownerId, PaymentRequestDeliveryRecipientBinding recipient,
        Uri endpoint, PaymentRequestDeliverySubscriptionStatus status, string keyId,
        string secretReference, IEnumerable<string> eventTypes,
        PaymentRequestDeliveryRetryPolicy retryPolicy,
        DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Subscription id cannot be empty.", nameof(id));
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        ArgumentNullException.ThrowIfNull(recipient);
        if (string.IsNullOrWhiteSpace(recipient.Kind) || string.IsNullOrWhiteSpace(recipient.Key))
            throw new ArgumentException("Recipient binding is required.", nameof(recipient));
        ValidateEndpoint(endpoint);
        ValidateKeyId(keyId);
        if (string.IsNullOrWhiteSpace(secretReference) || !SecretReferencePattern.IsMatch(secretReference))
            throw new ArgumentException("Secret reference must be an environment-style key name.", nameof(secretReference));
        var normalizedEvents = NormalizeEventTypes(eventTypes);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        retryPolicy.Validate();
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc)
            throw new ArgumentException("Updated timestamp cannot precede creation.", nameof(updatedAtUtc));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));

        return new PaymentRequestDeliverySubscription(
            id, ownerId, recipient, endpoint, status, keyId, secretReference,
            normalizedEvents, retryPolicy, createdAtUtc, updatedAtUtc);
    }

    public bool Authorizes(string eventType) =>
        EventTypes.Contains(NormalizeEventType(eventType), StringComparer.Ordinal);

    public void Disable(DateTimeOffset atUtc) => SetStatus(PaymentRequestDeliverySubscriptionStatus.Disabled, atUtc);
    public void Enable(DateTimeOffset atUtc) => SetStatus(PaymentRequestDeliverySubscriptionStatus.Active, atUtc);

    public static string NormalizeEventType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Event type is required.", nameof(value));
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 128 || normalized.Any(char.IsWhiteSpace))
            throw new ArgumentException("Event type is invalid.", nameof(value));
        return normalized;
    }

    private void SetStatus(PaymentRequestDeliverySubscriptionStatus status, DateTimeOffset atUtc)
    {
        EnsureUtc(atUtc, nameof(atUtc));
        if (atUtc < UpdatedAtUtc) throw new ArgumentException("Subscription timestamp cannot move backwards.", nameof(atUtc));
        Status = status;
        UpdatedAtUtc = atUtc;
    }

    private static IReadOnlyList<string> NormalizeEventTypes(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var normalized = values.Select(NormalizeEventType).Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (normalized.Length == 0) throw new ArgumentException("At least one event type is required.", nameof(values));
        if (normalized.Length > 64) throw new ArgumentException("At most 64 event types are allowed.", nameof(values));
        return normalized;
    }

    private static void ValidateEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri ||
            (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp) ||
            string.IsNullOrWhiteSpace(endpoint.Host) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("Endpoint must be an absolute HTTP/HTTPS URI without user info or fragment.", nameof(endpoint));
    }

    private static void ValidateKeyId(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 64 ||
            !keyId.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'))
            throw new ArgumentException("Webhook key id is invalid.", nameof(keyId));
    }

    private static void EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", name);
    }
}

public interface IPaymentRequestDeliverySubscriptionRegistry
{
    Task<PaymentRequestDeliverySubscription?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentRequestDeliverySubscription>> ListActiveAsync(
        PaymentRequestDeliveryRecipientBinding recipient, string eventType,
        CancellationToken cancellationToken = default);
    Task AddAsync(PaymentRequestDeliverySubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentRequestDeliverySubscription subscription, CancellationToken cancellationToken = default);
}

public sealed class PaymentRequestDeliverySubscriptionService(IPaymentRequestDeliverySubscriptionRegistry registry)
{
    public async Task<PaymentRequestDeliverySubscription> CreateAsync(
        Guid ownerId, RecipientReference recipient, Uri endpoint, string keyId, string secretReference,
        IEnumerable<string> eventTypes, PaymentRequestDeliveryRetryPolicy retryPolicy,
        DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscription = PaymentRequestDeliverySubscription.Create(
            ownerId, recipient, endpoint, keyId, secretReference, eventTypes, retryPolicy, createdAtUtc);
        await registry.AddAsync(subscription, cancellationToken);
        return subscription;
    }

    public async Task<PaymentRequestDeliverySubscription?> GetOwnedAsync(
        Guid ownerId, Guid id, CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        var value = await registry.GetAsync(id, cancellationToken);
        return value is not null && value.OwnerId == ownerId ? value : null;
    }

    public async Task<bool> SetActiveAsync(
        Guid ownerId, Guid id, bool active, DateTimeOffset atUtc,
        CancellationToken cancellationToken = default)
    {
        var value = await GetOwnedAsync(ownerId, id, cancellationToken);
        if (value is null) return false;
        if (active) value.Enable(atUtc); else value.Disable(atUtc);
        await registry.UpdateAsync(value, cancellationToken);
        return true;
    }
}
