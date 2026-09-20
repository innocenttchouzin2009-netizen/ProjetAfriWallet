using System.Globalization;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfNotificationPreferenceRepository(NotificationPreferenceDbContext dbContext)
    : INotificationPreferenceRepository
{
    public async Task<NotificationPreference?> GetAsync(
        Guid userId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        var entity = await dbContext.NotificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Channel == (int)channel, cancellationToken);

        return entity is null ? null : Restore(entity);
    }

    public async Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        var entities = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Channel)
            .ToListAsync(cancellationToken);

        return entities.Select(Restore).ToArray();
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.NotificationPreferences.Add(Map(preference));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.NotificationPreferences
            .SingleOrDefaultAsync(x => x.Id == preference.Id, cancellationToken)
            ?? throw new InvalidOperationException("Notification preference was not found.");

        if (entity.UserId != preference.UserId || entity.Channel != (int)preference.Channel)
            throw new InvalidOperationException("Notification preference identity cannot change.");

        entity.IsEnabled = preference.IsEnabled;
        entity.CreatedAtUtc = Format(preference.CreatedAtUtc);
        entity.UpdatedAtUtc = Format(preference.UpdatedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NotificationPreferenceEntity Map(NotificationPreference preference) => new()
    {
        Id = preference.Id,
        UserId = preference.UserId,
        Channel = (int)preference.Channel,
        IsEnabled = preference.IsEnabled,
        CreatedAtUtc = Format(preference.CreatedAtUtc),
        UpdatedAtUtc = Format(preference.UpdatedAtUtc)
    };

    private static NotificationPreference Restore(NotificationPreferenceEntity entity) =>
        NotificationPreference.Restore(
            entity.Id,
            entity.UserId,
            (NotificationChannel)entity.Channel,
            entity.IsEnabled,
            Parse(entity.CreatedAtUtc),
            Parse(entity.UpdatedAtUtc));

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
