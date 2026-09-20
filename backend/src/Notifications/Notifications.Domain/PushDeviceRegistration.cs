namespace AfriWallet.Notifications.Domain;

public enum PushPlatform
{
    Android = 1,
    Ios = 2
}

public readonly record struct PushDeviceRegistrationId
{
    public Guid Value { get; }
    private PushDeviceRegistrationId(Guid value) => Value = value;
    public static PushDeviceRegistrationId New() => new(Guid.NewGuid());
    public static PushDeviceRegistrationId From(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("Push device registration id cannot be empty.", nameof(value))
        : new(value);
}

public sealed class PushDeviceRegistration
{
    private PushDeviceRegistration(
        PushDeviceRegistrationId id,
        Guid userId,
        string installationId,
        PushPlatform platform,
        string pushToken,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool isActive,
        DateTimeOffset? deactivatedAtUtc)
    {
        Id = id;
        UserId = userId;
        InstallationId = installationId;
        Platform = platform;
        PushToken = pushToken;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        IsActive = isActive;
        DeactivatedAtUtc = deactivatedAtUtc;
    }

    public PushDeviceRegistrationId Id { get; }
    public Guid UserId { get; }
    public string InstallationId { get; }
    public PushPlatform Platform { get; }
    public string PushToken { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    public static PushDeviceRegistration Create(
        Guid userId,
        string installationId,
        PushPlatform platform,
        string pushToken,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        var normalizedInstallationId = NormalizeInstallationId(installationId);
        ValidatePlatform(platform);
        var normalizedToken = ValidateToken(pushToken);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new PushDeviceRegistration(
            PushDeviceRegistrationId.New(),
            userId,
            normalizedInstallationId,
            platform,
            normalizedToken,
            createdAtUtc,
            createdAtUtc,
            true,
            null);
    }

    public static PushDeviceRegistration Restore(
        PushDeviceRegistrationId id,
        Guid userId,
        string installationId,
        PushPlatform platform,
        string pushToken,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool isActive,
        DateTimeOffset? deactivatedAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        var normalizedInstallationId = NormalizeInstallationId(installationId);
        ValidatePlatform(platform);
        var normalizedToken = ValidateToken(pushToken);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc) throw new ArgumentException("Updated timestamp cannot be before creation time.", nameof(updatedAtUtc));

        if (isActive)
        {
            if (deactivatedAtUtc is not null) throw new ArgumentException("Active registration cannot have a deactivation timestamp.", nameof(deactivatedAtUtc));
        }
        else
        {
            if (deactivatedAtUtc is null) throw new ArgumentException("Inactive registration requires a deactivation timestamp.", nameof(deactivatedAtUtc));
            EnsureUtc(deactivatedAtUtc.Value, nameof(deactivatedAtUtc));
            if (deactivatedAtUtc.Value != updatedAtUtc) throw new ArgumentException("Inactive registration deactivation timestamp must equal updated timestamp.", nameof(deactivatedAtUtc));
        }

        return new PushDeviceRegistration(
            id,
            userId,
            normalizedInstallationId,
            platform,
            normalizedToken,
            createdAtUtc,
            updatedAtUtc,
            isActive,
            deactivatedAtUtc);
    }

    public void RotateToken(string pushToken, DateTimeOffset updatedAtUtc)
    {
        EnsureActive();
        EnsureForwardTime(updatedAtUtc, nameof(updatedAtUtc));
        PushToken = ValidateToken(pushToken);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTimeOffset deactivatedAtUtc)
    {
        EnsureActive();
        EnsureForwardTime(deactivatedAtUtc, nameof(deactivatedAtUtc));
        IsActive = false;
        DeactivatedAtUtc = deactivatedAtUtc;
        UpdatedAtUtc = deactivatedAtUtc;
    }

    public void Reactivate(string pushToken, DateTimeOffset reactivatedAtUtc)
    {
        if (IsActive) throw new InvalidOperationException("Push device registration is already active.");
        EnsureForwardTime(reactivatedAtUtc, nameof(reactivatedAtUtc));
        PushToken = ValidateToken(pushToken);
        IsActive = true;
        DeactivatedAtUtc = null;
        UpdatedAtUtc = reactivatedAtUtc;
    }

    private static string NormalizeInstallationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Installation id is required.", nameof(value));
        var normalized = value.Trim();
        if (normalized.Length > 128) throw new ArgumentException("Installation id cannot exceed 128 characters.", nameof(value));
        if (normalized.Any(char.IsWhiteSpace)) throw new ArgumentException("Installation id cannot contain whitespace.", nameof(value));
        return normalized;
    }

    private static string ValidateToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Push token is required.", nameof(value));
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("Push token cannot contain leading or trailing whitespace.", nameof(value));
        if (value.Length > 4096) throw new ArgumentException("Push token cannot exceed 4096 characters.", nameof(value));
        return value;
    }

    private static void ValidatePlatform(PushPlatform platform)
    {
        if (!Enum.IsDefined(platform)) throw new ArgumentOutOfRangeException(nameof(platform));
    }

    private void EnsureActive()
    {
        if (!IsActive) throw new InvalidOperationException("Push device registration is inactive.");
    }

    private void EnsureForwardTime(DateTimeOffset value, string parameterName)
    {
        EnsureUtc(value, parameterName);
        if (value < UpdatedAtUtc) throw new ArgumentException("Timestamp cannot move backwards.", parameterName);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
