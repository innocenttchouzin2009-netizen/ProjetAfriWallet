using System.Globalization;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfInAppNotificationRepository(NotificationInboxDbContext db) : IInAppNotificationRepository
{
    private const int MaxWriteAttempts = 4;

    public async Task<bool> AddAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        if (await db.Notifications.AsNoTracking().AnyAsync(
                x => x.UserId == notification.UserId && x.EventId == notification.EventId,
                cancellationToken))
            return false;

        var entity = MapEntity(notification);
        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            db.Notifications.Add(entity);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                db.Entry(entity).State = EntityState.Detached;
                return false;
            }
            catch (DbUpdateException exception) when (IsBusyOrLocked(exception) && attempt < MaxWriteAttempts)
            {
                db.Entry(entity).State = EntityState.Detached;
                await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt), cancellationToken);
                if (await db.Notifications.AsNoTracking().AnyAsync(
                        x => x.UserId == notification.UserId && x.EventId == notification.EventId,
                        cancellationToken))
                    return false;
            }
        }

        throw new InvalidOperationException("Notification delivery could not be persisted after bounded SQLite retries.");
    }

    public async Task<InAppNotification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var entity = await db.Notifications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == notificationId && x.ArchivedAtUtc == null, cancellationToken);
        return entity is null ? null : MapDomain(entity);
    }

    public async Task<IReadOnlyList<InAppNotification>> ListPageAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        NotificationInboxCursor? cursor,
        CancellationToken cancellationToken = default)
    {
        var query = db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId && x.ArchivedAtUtc == null);

        if (unreadOnly)
            query = query.Where(x => x.ReadAtUtc == null);

        if (cursor is not null)
        {
            var cursorSortKey = CreateSortKey(cursor.CreatedAtUtc, cursor.NotificationId);
            query = query.Where(x => x.SortKey.CompareTo(cursorSortKey) < 0);
        }

        var entities = await query
            .OrderByDescending(x => x.SortKey)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(MapDomain).ToArray();
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Notifications.AsNoTracking().CountAsync(
            x => x.UserId == userId && x.ArchivedAtUtc == null && x.ReadAtUtc == null,
            cancellationToken);

    public async Task UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var entity = await db.Notifications.SingleOrDefaultAsync(
            x => x.Id == notification.Id && x.UserId == notification.UserId,
            cancellationToken) ?? throw new InvalidOperationException("Notification was not found.");

        entity.ReadAtUtc = Format(notification.ReadAtUtc);
        entity.ArchivedAtUtc = Format(notification.ArchivedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ArchiveBeforeAsync(
        DateTimeOffset cutoffUtc,
        DateTimeOffset archivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureUtc(cutoffUtc, nameof(cutoffUtc));
        EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
        if (archivedAtUtc < cutoffUtc)
            throw new ArgumentException("Archive timestamp cannot precede cutoff.", nameof(archivedAtUtc));

        var cutoffText = cutoffUtc.ToString("O", CultureInfo.InvariantCulture);
        var archivedText = archivedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        return await db.Notifications
            .Where(x => x.ArchivedAtUtc == null && x.CreatedAtUtc.CompareTo(cutoffText) < 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ArchivedAtUtc, archivedText), cancellationToken);
    }

    private static InAppNotificationEntity MapEntity(InAppNotification notification) => new()
    {
        Id = notification.Id,
        UserId = notification.UserId,
        EventId = notification.EventId,
        PaymentRequestId = notification.PaymentRequestId,
        EventKind = (int)notification.EventKind,
        CreatedAtUtc = Format(notification.CreatedAtUtc)!,
        SortKey = CreateSortKey(notification.CreatedAtUtc, notification.Id),
        TransferId = notification.TransferId,
        ReadAtUtc = Format(notification.ReadAtUtc),
        ArchivedAtUtc = Format(notification.ArchivedAtUtc)
    };

    private static InAppNotification MapDomain(InAppNotificationEntity entity) => InAppNotification.Restore(
        entity.Id,
        entity.UserId,
        entity.EventId,
        entity.PaymentRequestId,
        (PaymentRequestEventKind)entity.EventKind,
        Parse(entity.CreatedAtUtc),
        entity.TransferId,
        ParseNullable(entity.ReadAtUtc),
        ParseNullable(entity.ArchivedAtUtc));

    internal static string CreateSortKey(DateTimeOffset createdAtUtc, Guid notificationId)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        if (notificationId == Guid.Empty)
            throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        return $"{createdAtUtc.UtcTicks:D19}|{notificationId:N}";
    }

    private static string? Format(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static DateTimeOffset? ParseNullable(string? value) =>
        value is null ? null : Parse(value);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode == 19;

    private static bool IsBusyOrLocked(DbUpdateException exception) =>
        exception.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode is 5 or 6;

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
