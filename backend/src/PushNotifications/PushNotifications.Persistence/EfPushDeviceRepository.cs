using AfriWallet.PushNotifications.Application;
using AfriWallet.PushNotifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PushNotifications.Persistence;

public sealed class EfPushDeviceRepository(PushNotificationDbContext dbContext) : IPushDeviceRepository
{
    public async Task<PushDeviceRegistration?> FindByUserAndDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.PushDevices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.PushDevices.Add(ToEntity(registration));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.PushDevices.SingleOrDefaultAsync(x => x.Id == registration.Id, cancellationToken)
            ?? throw new InvalidOperationException("Push device registration was not found.");
        Apply(registration, entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PushDeviceRegistration>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entities = await dbContext.PushDevices.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DeviceId)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    private static PushDeviceEntity ToEntity(PushDeviceRegistration value)
    {
        var entity = new PushDeviceEntity { Id = value.Id };
        Apply(value, entity);
        return entity;
    }

    private static void Apply(PushDeviceRegistration value, PushDeviceEntity entity)
    {
        entity.UserId = value.UserId;
        entity.DeviceId = value.DeviceId;
        entity.Platform = (int)value.Platform;
        entity.PushToken = value.PushToken;
        entity.Status = (int)value.Status;
        entity.RegisteredAtUtc = value.RegisteredAtUtc.ToString("O");
        entity.UpdatedAtUtc = value.UpdatedAtUtc.ToString("O");
        entity.RevokedAtUtc = value.RevokedAtUtc?.ToString("O");
    }

    private static PushDeviceRegistration ToDomain(PushDeviceEntity entity) =>
        PushDeviceRegistration.Restore(
            entity.Id,
            entity.UserId,
            entity.DeviceId,
            (PushPlatform)entity.Platform,
            entity.PushToken,
            (PushDeviceStatus)entity.Status,
            DateTimeOffset.Parse(entity.RegisteredAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(entity.UpdatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
            entity.RevokedAtUtc is null
                ? null
                : DateTimeOffset.Parse(entity.RevokedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));
}
