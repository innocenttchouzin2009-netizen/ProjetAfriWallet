using AfriWallet.PushNotifications.Domain;

namespace AfriWallet.PushNotifications.Application;

public sealed class PushDeviceRegistrationService(IPushDeviceRepository repository)
{
    public async Task<RegisterPushDeviceResult> RegisterAsync(RegisterPushDeviceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.UserId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(command));
        var normalizedDeviceId = NormalizeDeviceId(command.DeviceId);
        var existing = await repository.FindByUserAndDeviceAsync(command.UserId, normalizedDeviceId, cancellationToken);
        if (existing is null)
        {
            var created = PushDeviceRegistration.Register(command.UserId, normalizedDeviceId, command.Platform, command.PushToken, command.RegisteredAtUtc);
            await repository.AddAsync(created, cancellationToken);
            return new RegisterPushDeviceResult(RegisterPushDeviceStatus.Created, ToSnapshot(created));
        }
        existing.Refresh(command.Platform, command.PushToken, command.RegisteredAtUtc);
        await repository.UpdateAsync(existing, cancellationToken);
        return new RegisterPushDeviceResult(RegisterPushDeviceStatus.Refreshed, ToSnapshot(existing));
    }

    public async Task<IReadOnlyList<PushDeviceSnapshot>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();
        var devices = await repository.ListByUserAsync(userId, cancellationToken);
        return devices.Select(ToSnapshot).ToArray();
    }

    public async Task<PushDeviceSnapshot?> RevokeAsync(Guid userId, string deviceId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        var existing = await repository.FindByUserAndDeviceAsync(userId, normalizedDeviceId, cancellationToken);
        if (existing is null) return null;
        existing.Revoke(revokedAtUtc);
        await repository.UpdateAsync(existing, cancellationToken);
        return ToSnapshot(existing);
    }

    private static string NormalizeDeviceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Device id is required.", nameof(value));
        var normalized = value.Trim();
        if (normalized.Length > 128) throw new ArgumentException("Device id cannot exceed 128 characters.", nameof(value));
        return normalized;
    }

    private static PushDeviceSnapshot ToSnapshot(PushDeviceRegistration value) => new(value.Id, value.UserId, value.DeviceId, value.Platform, value.Status, value.RegisteredAtUtc, value.UpdatedAtUtc, value.RevokedAtUtc);
}
