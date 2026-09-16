namespace AfriWallet.Notifications.Application;

public sealed record PushNotificationMessage
{
    private PushNotificationMessage(
        Guid eventId,
        Guid recipientUserId,
        string title,
        string body,
        string? deepLink,
        DateTimeOffset createdAtUtc)
    {
        EventId = eventId;
        RecipientUserId = recipientUserId;
        Title = title;
        Body = body;
        DeepLink = deepLink;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid EventId { get; }
    public Guid RecipientUserId { get; }
    public string Title { get; }
    public string Body { get; }
    public string? DeepLink { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static PushNotificationMessage Create(
        Guid eventId,
        Guid recipientUserId,
        string title,
        string body,
        string? deepLink,
        DateTimeOffset createdAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Push event id cannot be empty.", nameof(eventId));
        }

        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Push recipient user id cannot be empty.", nameof(recipientUserId));
        }

        title = NormalizeRequired(title, 120, nameof(title));
        body = NormalizeRequired(body, 512, nameof(body));
        deepLink = NormalizeOptional(deepLink, 512, nameof(deepLink));

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Push notification timestamp must be UTC.", nameof(createdAtUtc));
        }

        return new PushNotificationMessage(eventId, recipientUserId, title, body, deepLink, createdAtUtc);
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        value = value.Trim();
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return value;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return value;
    }
}

public enum PushDeliveryDisposition
{
    Delivered = 1,
    RetryableFailure = 2,
    PermanentFailure = 3
}

public sealed record PushDeliveryResult(
    PushDeliveryDisposition Disposition,
    string? ProviderMessageId,
    string? ErrorCode,
    TimeSpan? RetryAfter)
{
    public static PushDeliveryResult Delivered(string? providerMessageId = null) =>
        new(PushDeliveryDisposition.Delivered, Normalize(providerMessageId), null, null);

    public static PushDeliveryResult Retryable(string errorCode, TimeSpan? retryAfter = null)
    {
        errorCode = RequireCode(errorCode);
        if (retryAfter is { } delay && delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter), "Retry delay cannot be negative.");
        }

        return new PushDeliveryResult(PushDeliveryDisposition.RetryableFailure, null, errorCode, retryAfter);
    }

    public static PushDeliveryResult Permanent(string errorCode) =>
        new(PushDeliveryDisposition.PermanentFailure, null, RequireCode(errorCode), null);

    private static string RequireCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Push delivery error code is required.", nameof(value));
        }

        value = value.Trim();
        if (value.Length > 128)
        {
            throw new ArgumentException("Push delivery error code cannot exceed 128 characters.", nameof(value));
        }

        return value;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface IPushNotificationTransport
{
    Task<PushDeliveryResult> SendAsync(
        PushNotificationMessage message,
        CancellationToken cancellationToken = default);
}

public sealed class PushNotificationDeliveryService(IPushNotificationTransport transport)
{
    public async Task<PushDeliveryResult> DeliverAsync(
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        return await transport.SendAsync(message, cancellationToken);
    }
}
