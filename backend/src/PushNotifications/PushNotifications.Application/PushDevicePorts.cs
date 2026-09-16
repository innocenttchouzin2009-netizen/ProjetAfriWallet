using AfriWallet.PushNotifications.Domain;

namespace AfriWallet.PushNotifications.Application;

public interface IPushDeviceRepository
{
    Task<PushDeviceRegistration?> FindByUserAndDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PushDeviceRegistration>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
