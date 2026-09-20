using AfriWallet.Notifications.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfInAppNotificationStore(NotificationDbContext dbContext)
    : IInAppNotificationReader
{
    public async Task DeliverAsync(
        InAppNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        Validate(notification);

        var existing = await dbContext.InAppNotifications
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SourceEventId == notification.SourceEventId, cancellationToken);

        if (existing is not null)
        {
            EnsureEquivalent(existing, notification);
            return;
        }

        dbContext.InAppNotifications.Add(ToEntity(notification));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var raced = await dbContext.InAppNotifications
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.SourceEventId == notification.SourceEventId, cancellationToken);

            if (raced is null)
            {
                throw;
            }

            EnsureEquivalent(raced, notification);
        }
    }

    public async Task<IReadOnlyList<InAppNotificationItem>> ListByRecipientAsync(
        Guid recipientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Recipient user id cannot be empty.", nameof(recipientUserId));
        }

        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var entities = await dbContext.InAppNotifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == recipientUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.NotificationId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(ToItem).ToArray();
    }

    public async Task<InAppNotificationItem?> GetAsync(
        Guid recipientUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Recipient user id cannot be empty.", nameof(recipientUserId));
        }

        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.InAppNotifications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NotificationId == notificationId && x.RecipientUserId == recipientUserId,
                cancellationToken);

        return entity is null ? null : ToItem(entity);
    }

    private static InAppNotificationEntity ToEntity(InAppNotification notification) => new()
    {
        NotificationId = notification.NotificationId,
        SourceEventId = notification.SourceEventId,
        RecipientUserId = notification.RecipientUserId,
        PaymentRequestId = notification.PaymentRequestId,
        EventType = notification.EventType,
        AmountMinor = notification.AmountMinor,
        CurrencyCode = notification.CurrencyCode,
        Title = notification.Title,
        Body = notification.Body,
        CreatedAtUtc = notification.CreatedAtUtc.ToString("O"),
        IsRead = false,
        ReadAtUtc = null
    };

    private static InAppNotificationItem ToItem(InAppNotificationEntity entity) => new(
        entity.NotificationId,
        entity.SourceEventId,
        entity.RecipientUserId,
        entity.PaymentRequestId,
        entity.EventType,
        entity.AmountMinor,
        entity.CurrencyCode,
        entity.Title,
        entity.Body,
        DateTimeOffset.Parse(entity.CreatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
        entity.IsRead,
        entity.ReadAtUtc is null
            ? null
            : DateTimeOffset.Parse(entity.ReadAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static void Validate(InAppNotification notification)
    {
        if (notification.NotificationId == Guid.Empty || notification.SourceEventId == Guid.Empty ||
            notification.RecipientUserId == Guid.Empty || notification.PaymentRequestId == Guid.Empty)
        {
            throw new ArgumentException("Notification, source event, recipient and payment request ids are required.", nameof(notification));
        }

        if (notification.Channel != NotificationChannel.InApp)
        {
            throw new NotSupportedException("Only In-App notification delivery is supported.");
        }

        if (notification.AmountMinor <= 0 || string.IsNullOrWhiteSpace(notification.CurrencyCode) ||
            string.IsNullOrWhiteSpace(notification.EventType) || string.IsNullOrWhiteSpace(notification.Title) ||
            string.IsNullOrWhiteSpace(notification.Body))
        {
            throw new ArgumentException("Notification content is invalid.", nameof(notification));
        }

        if (notification.CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Notification timestamp must be UTC.", nameof(notification));
        }
    }

    private static void EnsureEquivalent(InAppNotificationEntity existing, InAppNotification notification)
    {
        if (existing.NotificationId != notification.NotificationId ||
            existing.RecipientUserId != notification.RecipientUserId ||
            existing.PaymentRequestId != notification.PaymentRequestId ||
            !string.Equals(existing.EventType, notification.EventType, StringComparison.Ordinal) ||
            existing.AmountMinor != notification.AmountMinor ||
            !string.Equals(existing.CurrencyCode, notification.CurrencyCode, StringComparison.Ordinal) ||
            !string.Equals(existing.Title, notification.Title, StringComparison.Ordinal) ||
            !string.Equals(existing.Body, notification.Body, StringComparison.Ordinal) ||
            existing.CreatedAtUtc != notification.CreatedAtUtc.ToString("O"))
        {
            throw new InvalidOperationException("Source event id is already associated with a different In-App notification.");
        }
    }
}
