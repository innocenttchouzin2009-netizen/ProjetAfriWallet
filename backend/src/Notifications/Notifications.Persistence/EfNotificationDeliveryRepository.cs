using System.Globalization;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfNotificationDeliveryRepository(NotificationDeliveryDbContext dbContext)
    : INotificationDeliveryRepository
{
    public async Task<NotificationDelivery?> GetAsync(
        Guid eventId,
        NotificationChannel channel,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        if (!Enum.IsDefined(channel)) throw new ArgumentOutOfRangeException(nameof(channel));
        if (recipientUserId == Guid.Empty) throw new ArgumentException("Recipient user id cannot be empty.", nameof(recipientUserId));

        var entity = await dbContext.Deliveries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EventId == eventId &&
                     x.Channel == (int)channel &&
                     x.RecipientUserId == recipientUserId,
                cancellationToken);

        return entity is null ? null : Restore(entity);
    }

    public async Task<NotificationDelivery> GetOrAddAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await GetAsync(
            delivery.EventId,
            delivery.Channel,
            delivery.RecipientUserId,
            cancellationToken);
        if (existing is not null)
        {
            EnsureEquivalent(existing, delivery);
            return existing;
        }

        dbContext.Deliveries.Add(Map(delivery));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return delivery;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var raced = await GetAsync(
                delivery.EventId,
                delivery.Channel,
                delivery.RecipientUserId,
                cancellationToken);
            if (raced is null) throw;
            EnsureEquivalent(raced, delivery);
            return raced;
        }
    }

    public async Task MarkDispatchedAsync(
        Guid deliveryId,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (deliveryId == Guid.Empty) throw new ArgumentException("Delivery id cannot be empty.", nameof(deliveryId));
        if (dispatchedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Dispatch timestamp must be UTC.", nameof(dispatchedAtUtc));

        var entity = await dbContext.Deliveries
            .SingleOrDefaultAsync(x => x.DeliveryId == deliveryId, cancellationToken)
            ?? throw new InvalidOperationException("Notification delivery was not found.");

        if (entity.Status == (int)NotificationDeliveryStatus.Dispatched)
            return;

        entity.Status = (int)NotificationDeliveryStatus.Dispatched;
        entity.DispatchedAtUtc = Format(dispatchedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDeliveryEntity Map(NotificationDelivery delivery) => new()
    {
        DeliveryId = delivery.DeliveryId,
        EventId = delivery.EventId,
        Channel = (int)delivery.Channel,
        RecipientUserId = delivery.RecipientUserId,
        PaymentRequestId = delivery.PaymentRequestId,
        EventKind = (int)delivery.EventKind,
        CreatedAtUtc = Format(delivery.CreatedAtUtc),
        TransferId = delivery.TransferId,
        Status = (int)delivery.Status,
        DispatchedAtUtc = delivery.DispatchedAtUtc is null ? null : Format(delivery.DispatchedAtUtc.Value)
    };

    private static NotificationDelivery Restore(NotificationDeliveryEntity entity) => new(
        entity.DeliveryId,
        entity.EventId,
        (NotificationChannel)entity.Channel,
        entity.RecipientUserId,
        entity.PaymentRequestId,
        (PaymentRequestEventKind)entity.EventKind,
        Parse(entity.CreatedAtUtc),
        entity.TransferId,
        (NotificationDeliveryStatus)entity.Status,
        entity.DispatchedAtUtc is null ? null : Parse(entity.DispatchedAtUtc));

    private static void EnsureEquivalent(NotificationDelivery existing, NotificationDelivery candidate)
    {
        if (existing.PaymentRequestId != candidate.PaymentRequestId ||
            existing.EventKind != candidate.EventKind ||
            existing.CreatedAtUtc != candidate.CreatedAtUtc ||
            existing.TransferId != candidate.TransferId)
        {
            throw new InvalidOperationException(
                "Event id, channel and recipient are already associated with a different notification delivery.");
        }
    }

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
