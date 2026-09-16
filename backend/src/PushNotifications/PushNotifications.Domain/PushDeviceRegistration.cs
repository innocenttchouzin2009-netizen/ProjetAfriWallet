namespace AfriWallet.PushNotifications.Domain;

public sealed class PushDeviceRegistration
{
    private PushDeviceRegistration(
        Guid id,
        Guid userId,
        string deviceId,
        PushPlatform platform,
        string pushToken,
        PushDeviceStatus status,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? revokedAtUtc)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        Platform = platform;
        PushToken = pushToken;
        Status = status;
        RegisteredAtUtc = registeredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RevokedAtUtc = revokedAtUtc;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string DeviceId { get; }
    public PushPlatform Platform { get; private set; }
    public string PushToken { get; private set; }
    public PushDeviceStatus Status { get; private set; }
    public DateTimeOffset RegisteredAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static PushDeviceRegistration Register(
        Guid userId,
        string deviceId,
        PushPlatform platform,
        string pushToken,
        DateTimeOffset registeredAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        ValidatePlatform(platform);
        EnsureUtc(registeredAtUtc, nameof(registeredAtUtc));
        return new PushDeviceRegistration(
            Guid.NewGuid(),
            userId,
            NormalizeDeviceId(deviceId),
            platform,
            NormalizeToken(pushToken),
            PushDeviceStatus.Active,
            registeredAtUtc,
            registeredAtUtc,
            null);
    }

    public static PushDeviceRegistration Restore(
        Guid id,
        Guid userId,
        string deviceId,
        PushPlatform platform,
        string pushToken,
        PushDeviceStatus status,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? revokedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Registration id cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        ValidatePlatform(platform);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        EnsureUtc(registeredAtUtc, nameof(registeredAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < registeredAtUtc)
            throw new ArgumentException("Updated timestamp cannot be earlier than registration timestamp.", nameof(updatedAtUtc));
        if (revokedAtUtc is not null)
        {
            EnsureUtc(revokedAtUtc.Value, nameof(revokedAtUtc));
            if (revokedAtUtc.Value < registeredAtUtc || revokedAtUtc.Value > updatedAtUtc)
                throw new ArgumentException("Revocation timestamp must be within registration lifecycle.", nameof(revokedAtUtc));
        }
        if (status == PushDeviceStatus.Revoked && revokedAtUtc is null)
            throw new ArgumentException("Revoked registrations require a revocation timestamp.", nameof(revokedAtUtc));
        if (status == PushDeviceStatus.Active && revokedAtUtc is not null)
            throw new ArgumentException("Active registrations cannot have a revocation timestamp.", nameof(revokedAtUtc));

        return new PushDeviceRegistration(
            id,
            userId,
            NormalizeDeviceId(deviceId),
            platform,
            NormalizeToken(pushToken),
            status,
            registeredAtUtc,
            updatedAtUtc,
            revokedAtUtc);
    }

    public void Refresh(PushPlatform platform, string pushToken, DateTimeOffset updatedAtUtc)
    {
        EnsureTransitionTime(updatedAtUtc, nameof(updatedAtUtc));
        ValidatePlatform(platform);
        Platform = platform;
        PushToken = NormalizeToken(pushToken);
        Status = PushDeviceStatus.Active;
        RevokedAtUtc = null;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        EnsureTransitionTime(revokedAtUtc, nameof(revokedAtUtc));
        if (Status == PushDeviceStatus.Revoked) return;
        Status = PushDeviceStatus.Revoked;
        RevokedAtUtc = revokedAtUtc;
        UpdatedAtUtc = revokedAtUtc;
    }

    private void EnsureTransitionTime(DateTimeOffset value, string parameterName)
    {
        EnsureUtc(value, parameterName);
        if (value < UpdatedAtUtc)
            throw new ArgumentException("Device registration timestamp cannot move backwards.", parameterName);
    }

    private static string NormalizeDeviceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Device id is required.", nameof(value));
        var normalized = value.Trim();
        if (normalized.Length > 128) throw new ArgumentException("Device id cannot exceed 128 characters.", nameof(value));
        return normalized;
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Push token is required.", nameof(value));
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("Push token cannot contain surrounding whitespace.", nameof(value));
        if (value.Length > 4096) throw new ArgumentException("Push token cannot exceed 4096 characters.", nameof(value));
        return value;
    }

    private static void ValidatePlatform(PushPlatform platform)
    {
        if (!Enum.IsDefined(platform)) throw new ArgumentOutOfRangeException(nameof(platform));
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
