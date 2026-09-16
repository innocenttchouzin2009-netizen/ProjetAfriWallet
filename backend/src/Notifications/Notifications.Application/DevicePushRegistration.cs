using System.Security.Cryptography;
using System.Text;

namespace AfriWallet.Notifications.Application;

public enum PushDevicePlatform
{
    Android = 1,
    Ios = 2,
    Web = 3
}

public sealed class DevicePushRegistration
{
    private DevicePushRegistration(
        Guid id,
        Guid userId,
        string deviceId,
        PushDevicePlatform platform,
        string token,
        string tokenHash,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset lastSeenAtUtc,
        DateTimeOffset? revokedAtUtc)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        Platform = platform;
        Token = token;
        TokenHash = tokenHash;
        RegisteredAtUtc = registeredAtUtc;
        LastSeenAtUtc = lastSeenAtUtc;
        RevokedAtUtc = revokedAtUtc;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string DeviceId { get; }
    public PushDevicePlatform Platform { get; }
    public string Token { get; }
    public string TokenHash { get; }
    public DateTimeOffset RegisteredAtUtc { get; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool IsActive => RevokedAtUtc is null;

    public static DevicePushRegistration Create(
        Guid userId,
        string deviceId,
        PushDevicePlatform platform,
        string token,
        DateTimeOffset registeredAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Push registration user id cannot be empty.", nameof(userId));
        }

        deviceId = NormalizeDeviceId(deviceId);
        ValidatePlatform(platform);
        token = NormalizeToken(token);
        EnsureUtc(registeredAtUtc, nameof(registeredAtUtc));

        return new DevicePushRegistration(
            Guid.NewGuid(),
            userId,
            deviceId,
            platform,
            token,
            PushTokenFingerprint.Compute(token),
            registeredAtUtc,
            registeredAtUtc,
            null);
    }

    public static DevicePushRegistration Restore(
        Guid id,
        Guid userId,
        string deviceId,
        PushDevicePlatform platform,
        string token,
        string tokenHash,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset lastSeenAtUtc,
        DateTimeOffset? revokedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Push registration id cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("Push registration user id cannot be empty.", nameof(userId));

        deviceId = NormalizeDeviceId(deviceId);
        ValidatePlatform(platform);
        token = NormalizeToken(token);
        EnsureUtc(registeredAtUtc, nameof(registeredAtUtc));
        EnsureUtc(lastSeenAtUtc, nameof(lastSeenAtUtc));
        if (lastSeenAtUtc < registeredAtUtc) throw new ArgumentException("Last-seen timestamp cannot precede registration.", nameof(lastSeenAtUtc));
        if (revokedAtUtc is not null)
        {
            EnsureUtc(revokedAtUtc.Value, nameof(revokedAtUtc));
            if (revokedAtUtc.Value < lastSeenAtUtc) throw new ArgumentException("Revocation cannot precede last-seen timestamp.", nameof(revokedAtUtc));
        }

        var expectedHash = PushTokenFingerprint.Compute(token);
        if (!string.Equals(expectedHash, tokenHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Stored push token fingerprint does not match the protected token.");
        }

        return new DevicePushRegistration(id, userId, deviceId, platform, token, tokenHash, registeredAtUtc, lastSeenAtUtc, revokedAtUtc);
    }

    public void Touch(DateTimeOffset seenAtUtc)
    {
        EnsureActive();
        EnsureUtc(seenAtUtc, nameof(seenAtUtc));
        if (seenAtUtc < LastSeenAtUtc) throw new ArgumentException("Last-seen timestamp cannot move backwards.", nameof(seenAtUtc));
        LastSeenAtUtc = seenAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        EnsureActive();
        EnsureUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (revokedAtUtc < LastSeenAtUtc) throw new ArgumentException("Revocation cannot precede last-seen timestamp.", nameof(revokedAtUtc));
        RevokedAtUtc = revokedAtUtc;
    }

    private void EnsureActive()
    {
        if (!IsActive) throw new InvalidOperationException("Push registration is already revoked.");
    }

    private static string NormalizeDeviceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Device id is required.", nameof(value));
        value = value.Trim();
        if (value.Length > 128) throw new ArgumentException("Device id cannot exceed 128 characters.", nameof(value));
        if (value.Any(char.IsWhiteSpace)) throw new ArgumentException("Device id cannot contain whitespace.", nameof(value));
        return value;
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Push token is required.", nameof(value));
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Push token cannot contain surrounding whitespace.", nameof(value));
        if (value.Length > 4096) throw new ArgumentException("Push token cannot exceed 4096 characters.", nameof(value));
        if (value.Any(char.IsWhiteSpace)) throw new ArgumentException("Push token cannot contain whitespace.", nameof(value));
        return value;
    }

    private static void ValidatePlatform(PushDevicePlatform platform)
    {
        if (!Enum.IsDefined(platform)) throw new ArgumentOutOfRangeException(nameof(platform));
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}

public static class PushTokenFingerprint
{
    public static string Compute(string token)
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentException("Push token is required.", nameof(token));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

public sealed record RegisterDevicePushTokenCommand(
    Guid UserId,
    string DeviceId,
    PushDevicePlatform Platform,
    string Token,
    DateTimeOffset RegisteredAtUtc);

public enum RegisterDevicePushTokenStatus
{
    Created = 1,
    Existing = 2,
    Replaced = 3
}

public sealed record DevicePushRegistrationSnapshot(
    Guid Id,
    Guid UserId,
    string DeviceId,
    PushDevicePlatform Platform,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset? RevokedAtUtc);

public sealed record RegisterDevicePushTokenResult(
    RegisterDevicePushTokenStatus Status,
    DevicePushRegistrationSnapshot Registration);

public interface IDevicePushRegistrationRepository
{
    Task<DevicePushRegistration?> GetActiveByDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default);
    Task<DevicePushRegistration?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DevicePushRegistration>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(DevicePushRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateAsync(DevicePushRegistration registration, CancellationToken cancellationToken = default);
}

public sealed class DevicePushRegistrationService(IDevicePushRegistrationRepository repository)
{
    public async Task<RegisterDevicePushTokenResult> RegisterAsync(
        RegisterDevicePushTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var candidate = DevicePushRegistration.Create(
            command.UserId,
            command.DeviceId,
            command.Platform,
            command.Token,
            command.RegisteredAtUtc);

        var currentDevice = await repository.GetActiveByDeviceAsync(candidate.UserId, candidate.DeviceId, cancellationToken);
        if (currentDevice is not null && string.Equals(currentDevice.TokenHash, candidate.TokenHash, StringComparison.Ordinal))
        {
            if (currentDevice.Platform != candidate.Platform)
            {
                throw new InvalidOperationException("An active device cannot change platform without replacing its push token.");
            }

            currentDevice.Touch(command.RegisteredAtUtc);
            await repository.UpdateAsync(currentDevice, cancellationToken);
            return new RegisterDevicePushTokenResult(RegisterDevicePushTokenStatus.Existing, ToSnapshot(currentDevice));
        }

        var currentToken = await repository.GetActiveByTokenHashAsync(candidate.TokenHash, cancellationToken);
        var replaced = false;

        if (currentToken is not null)
        {
            currentToken.Revoke(command.RegisteredAtUtc);
            await repository.UpdateAsync(currentToken, cancellationToken);
            replaced = true;
        }

        if (currentDevice is not null && (currentToken is null || currentDevice.Id != currentToken.Id))
        {
            currentDevice.Revoke(command.RegisteredAtUtc);
            await repository.UpdateAsync(currentDevice, cancellationToken);
            replaced = true;
        }

        await repository.AddAsync(candidate, cancellationToken);
        return new RegisterDevicePushTokenResult(
            replaced ? RegisterDevicePushTokenStatus.Replaced : RegisterDevicePushTokenStatus.Created,
            ToSnapshot(candidate));
    }

    public async Task<bool> RevokeAsync(
        Guid userId,
        string deviceId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();

        var registration = await repository.GetActiveByDeviceAsync(userId, deviceId, cancellationToken);
        if (registration is null) return false;

        registration.Revoke(revokedAtUtc);
        await repository.UpdateAsync(registration, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DevicePushRegistrationSnapshot>> ListActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();
        var values = await repository.ListActiveByUserAsync(userId, cancellationToken);
        return values.Select(ToSnapshot).ToArray();
    }

    private static DevicePushRegistrationSnapshot ToSnapshot(DevicePushRegistration value) => new(
        value.Id,
        value.UserId,
        value.DeviceId,
        value.Platform,
        value.RegisteredAtUtc,
        value.LastSeenAtUtc,
        value.RevokedAtUtc);
}
