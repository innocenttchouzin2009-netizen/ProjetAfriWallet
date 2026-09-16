namespace AfriWallet.Notifications.Domain;

public sealed class NotificationPreference
{
    private NotificationPreference(
        Guid id,
        Guid userId,
        NotificationChannel channel,
        bool isEnabled,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Channel = channel;
        IsEnabled = isEnabled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public NotificationChannel Channel { get; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static NotificationPreference New(
        Guid userId,
        NotificationChannelPolicy policy,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        return new NotificationPreference(
            Guid.NewGuid(),
            userId,
            policy.Channel,
            policy.DefaultEnabled,
            createdAtUtc,
            createdAtUtc);
    }

    public static NotificationPreference Restore(
        Guid id,
        Guid userId,
        NotificationChannel channel,
        bool isEnabled,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Notification preference id cannot be empty.", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc)
            throw new ArgumentException("Updated timestamp cannot precede creation.", nameof(updatedAtUtc));

        return new NotificationPreference(id, userId, channel, isEnabled, createdAtUtc, updatedAtUtc);
    }

    public bool SetEnabled(
        bool enabled,
        NotificationChannelPolicy policy,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < UpdatedAtUtc)
            throw new ArgumentException("Updated timestamp cannot move backwards.", nameof(updatedAtUtc));
        if (policy.Channel != Channel)
            throw new InvalidOperationException("Channel policy does not match the notification preference channel.");
        if (!policy.UserConfigurable && enabled != policy.DefaultEnabled)
            throw new InvalidOperationException("Notification channel is not user configurable.");
        if (IsEnabled == enabled)
            return false;

        IsEnabled = enabled;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
