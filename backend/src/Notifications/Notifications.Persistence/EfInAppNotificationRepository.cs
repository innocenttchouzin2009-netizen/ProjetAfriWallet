using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfInAppNotificationRepository(NotificationInboxDbContext dbContext) : IInAppNotificationRepository
{
    public async Task<IReadOnlyList<InAppNotification>> ListAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        if (unreadOnly) query = query.Where(x => x.ReadAtUtc == null);
        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    public async Task<InAppNotification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notifications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.CountAsync(x => x.UserId == userId && x.ReadAtUtc == null, cancellationToken);

    public async Task AddAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var exists = await dbContext.Notifications.AsNoTracking()
            .AnyAsync(x => x.UserId == notification.UserId && x.EventId == notification.EventId, cancellationToken);
        if (exists) return;

        dbContext.Notifications.Add(ToEntity(notification));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var duplicate = await dbContext.Notifications.AsNoTracking()
                .AnyAsync(x => x.UserId == notification.UserId && x.EventId == notification.EventId, cancellationToken);
            if (!duplicate) throw;
        }
    }

    public async Task UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var entity = await dbContext.Notifications
            .SingleOrDefaultAsync(x => x.Id == notification.Id && x.UserId == notification.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Notification was not found.");
        entity.ReadAtUtc = notification.ReadAtUtc?.ToString("O");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static InAppNotificationEntity ToEntity(InAppNotification item) => new()
    {
        Id = item.Id,
        UserId = item.UserId,
        EventId = item.EventId,
        PaymentRequestId = item.PaymentRequestId,
        EventKind = (int)item.EventKind,
        CreatedAtUtc = item.CreatedAtUtc.ToString("O"),
        TransferId = item.TransferId,
        ReadAtUtc = item.ReadAtUtc?.ToString("O")
    };

    private static InAppNotification ToDomain(InAppNotificationEntity entity) =>
        InAppNotification.Restore(
            entity.Id,
            entity.UserId,
            entity.EventId,
            entity.PaymentRequestId,
            (PaymentRequestEventKind)entity.EventKind,
            DateTimeOffset.Parse(entity.CreatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
            entity.TransferId,
            entity.ReadAtUtc is null
                ? null
                : DateTimeOffset.Parse(entity.ReadAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));
}
