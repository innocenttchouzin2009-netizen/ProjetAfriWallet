using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public interface IPushDeviceRegistrationRepository
{
    Task<PushDeviceRegistration?> FindByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PushDeviceRegistration>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PushDeviceRegistration registration,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PushDeviceRegistration registration,
        CancellationToken cancellationToken = default);
}

public interface IPushDeliveryPort
{
    Task<PushDeliveryResult> DeliverAsync(
        PushDeliveryTarget target,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default);
}

public sealed class PushDeviceRegistrationService(IPushDeviceRegistrationRepository repository)
{
    public async Task<RegisterPushDeviceResult> RegisterAsync(
        RegisterPushDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var candidate = PushDeviceRegistration.Create(
            command.UserId,
            command.InstallationId,
            command.Platform,
            command.PushToken,
            command.RegisteredAtUtc);

        var existing = await repository.FindByInstallationIdAsync(candidate.InstallationId, cancellationToken);
        if (existing is null)
        {
            await repository.AddAsync(candidate, cancellationToken);
            return new RegisterPushDeviceResult(RegisterPushDeviceStatus.Registered, ToSnapshot(candidate));
        }

        if (existing.UserId != command.UserId)
            throw new InvalidOperationException("Push installation is already registered to another user.");
        if (existing.Platform != command.Platform)
            throw new InvalidOperationException("Push installation platform cannot change.");

        if (!existing.IsActive)
        {
            existing.Reactivate(command.PushToken, command.RegisteredAtUtc);
            await repository.UpdateAsync(existing, cancellationToken);
            return new RegisterPushDeviceResult(RegisterPushDeviceStatus.Reactivated, ToSnapshot(existing));
        }

        if (string.Equals(existing.PushToken, command.PushToken, StringComparison.Ordinal))
            return new RegisterPushDeviceResult(RegisterPushDeviceStatus.Existing, ToSnapshot(existing));

        existing.RotateToken(command.PushToken, command.RegisteredAtUtc);
        await repository.UpdateAsync(existing, cancellationToken);
        return new RegisterPushDeviceResult(RegisterPushDeviceStatus.TokenRotated, ToSnapshot(existing));
    }

    public async Task<bool> UnregisterAsync(
        Guid userId,
        string installationId,
        DateTimeOffset deactivatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await repository.FindByInstallationIdAsync(installationId, cancellationToken);
        if (existing is null || existing.UserId != userId || !existing.IsActive) return false;

        existing.Deactivate(deactivatedAtUtc);
        await repository.UpdateAsync(existing, cancellationToken);
        return true;
    }

    private static PushDeviceRegistrationSnapshot ToSnapshot(PushDeviceRegistration registration) => new(
        registration.Id,
        registration.UserId,
        registration.InstallationId,
        registration.Platform,
        registration.PushToken,
        registration.CreatedAtUtc,
        registration.UpdatedAtUtc,
        registration.IsActive,
        registration.DeactivatedAtUtc);
}
