using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public sealed record RegisterPushDeviceCommand(
    Guid UserId,
    string InstallationId,
    PushPlatform Platform,
    string PushToken,
    DateTimeOffset RegisteredAtUtc);

public enum RegisterPushDeviceStatus
{
    Registered = 1,
    Existing = 2,
    TokenRotated = 3,
    Reactivated = 4
}

public sealed record PushDeviceRegistrationSnapshot(
    PushDeviceRegistrationId Id,
    Guid UserId,
    string InstallationId,
    PushPlatform Platform,
    string PushToken,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool IsActive,
    DateTimeOffset? DeactivatedAtUtc);

public sealed record RegisterPushDeviceResult(
    RegisterPushDeviceStatus Status,
    PushDeviceRegistrationSnapshot Registration);

public sealed record PushDeliveryTarget(
    PushDeviceRegistrationId RegistrationId,
    Guid UserId,
    PushPlatform Platform,
    string PushToken);

public sealed record PushNotificationMessage(
    Guid NotificationId,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data)
{
    public static PushNotificationMessage Create(
        Guid notificationId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null)
    {
        if (notificationId == Guid.Empty) throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Push title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Push body is required.", nameof(body));
        if (title.Length > 160) throw new ArgumentException("Push title cannot exceed 160 characters.", nameof(title));
        if (body.Length > 2048) throw new ArgumentException("Push body cannot exceed 2048 characters.", nameof(body));
        return new PushNotificationMessage(notificationId, title.Trim(), body.Trim(), data ?? new Dictionary<string, string>());
    }
}

public enum PushDeliveryFailureKind
{
    InvalidToken = 1,
    Transient = 2,
    Permanent = 3
}

public sealed record PushDeliveryResult(
    bool Accepted,
    string? ProviderMessageId,
    PushDeliveryFailureKind? FailureKind)
{
    public static PushDeliveryResult Delivered(string? providerMessageId = null) => new(true, providerMessageId, null);
    public static PushDeliveryResult Failed(PushDeliveryFailureKind failureKind) => new(false, null, failureKind);
}
