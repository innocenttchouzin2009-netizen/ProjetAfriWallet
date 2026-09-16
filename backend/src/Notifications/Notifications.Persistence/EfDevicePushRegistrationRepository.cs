using AfriWallet.Notifications.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfDevicePushRegistrationRepository(
    NotificationInboxDbContext dbContext,
    IPushTokenProtector tokenProtector) : IDevicePushRegistrationRepository
{
    public async Task<DevicePushRegistration?> GetActiveByDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DevicePushRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId && x.RevokedAtUtc == null, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<DevicePushRegistration?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DevicePushRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<DevicePushRegistration>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.DevicePushRegistrations
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .OrderBy(x => x.DeviceId)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    public async Task AddAsync(DevicePushRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        dbContext.DevicePushRegistrations.Add(ToEntity(registration));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DevicePushRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var entity = await dbContext.DevicePushRegistrations
            .SingleOrDefaultAsync(x => x.Id == registration.Id, cancellationToken)
            ?? throw new InvalidOperationException("Push registration was not found.");

        entity.UserId = registration.UserId;
        entity.DeviceId = registration.DeviceId;
        entity.Platform = (int)registration.Platform;
        entity.TokenHash = registration.TokenHash;
        entity.ProtectedToken = tokenProtector.Protect(registration.Token);
        entity.RegisteredAtUtc = Format(registration.RegisteredAtUtc);
        entity.LastSeenAtUtc = Format(registration.LastSeenAtUtc);
        entity.RevokedAtUtc = registration.RevokedAtUtc is null ? null : Format(registration.RevokedAtUtc.Value);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private DevicePushRegistration ToDomain(DevicePushRegistrationEntity entity) =>
        DevicePushRegistration.Restore(
            entity.Id,
            entity.UserId,
            entity.DeviceId,
            (PushDevicePlatform)entity.Platform,
            tokenProtector.Unprotect(entity.ProtectedToken),
            entity.TokenHash,
            Parse(entity.RegisteredAtUtc),
            Parse(entity.LastSeenAtUtc),
            entity.RevokedAtUtc is null ? null : Parse(entity.RevokedAtUtc));

    private DevicePushRegistrationEntity ToEntity(DevicePushRegistration registration) => new()
    {
        Id = registration.Id,
        UserId = registration.UserId,
        DeviceId = registration.DeviceId,
        Platform = (int)registration.Platform,
        TokenHash = registration.TokenHash,
        ProtectedToken = tokenProtector.Protect(registration.Token),
        RegisteredAtUtc = Format(registration.RegisteredAtUtc),
        LastSeenAtUtc = Format(registration.LastSeenAtUtc),
        RevokedAtUtc = registration.RevokedAtUtc is null ? null : Format(registration.RevokedAtUtc.Value)
    };

    private static string Format(DateTimeOffset value) => value.ToString("O");
    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
