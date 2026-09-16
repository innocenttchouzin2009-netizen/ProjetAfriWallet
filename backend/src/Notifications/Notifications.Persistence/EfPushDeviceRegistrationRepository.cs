using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfPushDeviceRegistrationRepository(PushDeviceRegistrationDbContext dbContext)
    : IPushDeviceRegistrationRepository
{
    public async Task<PushDeviceRegistration?> FindByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(installationId)) throw new ArgumentException("Installation id is required.", nameof(installationId));
        var normalized = installationId.Trim();
        var entity = await dbContext.PushDeviceRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.InstallationId == normalized, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<PushDeviceRegistration>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        var entities = await dbContext.PushDeviceRegistrations
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.InstallationId)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    public async Task AddAsync(
        PushDeviceRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        dbContext.PushDeviceRegistrations.Add(ToEntity(registration));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PushDeviceRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var entity = await dbContext.PushDeviceRegistrations
            .SingleOrDefaultAsync(x => x.Id == registration.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException("Push device registration was not found.");

        entity.PushToken = registration.PushToken;
        entity.UpdatedAtUtc = Format(registration.UpdatedAtUtc);
        entity.IsActive = registration.IsActive;
        entity.DeactivatedAtUtc = registration.DeactivatedAtUtc is null ? null : Format(registration.DeactivatedAtUtc.Value);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PushDeviceRegistrationEntity ToEntity(PushDeviceRegistration registration) => new()
    {
        Id = registration.Id.Value,
        UserId = registration.UserId,
        InstallationId = registration.InstallationId,
        Platform = registration.Platform,
        PushToken = registration.PushToken,
        CreatedAtUtc = Format(registration.CreatedAtUtc),
        UpdatedAtUtc = Format(registration.UpdatedAtUtc),
        IsActive = registration.IsActive,
        DeactivatedAtUtc = registration.DeactivatedAtUtc is null ? null : Format(registration.DeactivatedAtUtc.Value)
    };

    private static PushDeviceRegistration ToDomain(PushDeviceRegistrationEntity entity) =>
        PushDeviceRegistration.Restore(
            PushDeviceRegistrationId.From(entity.Id),
            entity.UserId,
            entity.InstallationId,
            entity.Platform,
            entity.PushToken,
            Parse(entity.CreatedAtUtc),
            Parse(entity.UpdatedAtUtc),
            entity.IsActive,
            entity.DeactivatedAtUtc is null ? null : Parse(entity.DeactivatedAtUtc));

    private static string Format(DateTimeOffset value) => value.ToUniversalTime().ToString("O");
    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
}
