using AfriWallet.Notifications.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class EfNotificationDeliveryRepository(NotificationDbContext dbContext)
    : INotificationDeliveryRepository
{
    public async Task<NotificationDelivery?> GetAsync(
        Guid eventId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));

        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.NotificationDeliveries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EventId == eventId && x.Channel == (int)channel,
                cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<NotificationDelivery> GetOrAddAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await GetAsync(delivery.EventId, delivery.Channel, cancellationToken);
        if (existing is not null)
        {
            EnsureEquivalent(existing, delivery);
            return existing;
        }

        dbContext.NotificationDeliveries.Add(ToEntity(delivery));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return delivery;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var raced = await GetAsync(delivery.EventId, delivery.Channel, cancellationToken);
            if (raced is null)
                throw;

            EnsureEquivalent(raced, delivery);
            return raced;
        }
    }

    public async Task MarkDispatchedAsync(
        Guid deliveryId,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (deliveryId == Guid.Empty)
            throw new ArgumentException("Delivery id cannot be empty.", nameof(deliveryId));
        if (dispatchedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Dispatch timestamp must be UTC.", nameof(dispatchedAtUtc));

        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.NotificationDeliveries
            .SingleOrDefaultAsync(x => x.DeliveryId == deliveryId, cancellationToken)
            ?? throw new InvalidOperationException("Notification delivery was not found.");

        if (entity.Status == (int)NotificationDeliveryStatus.Dispatched)
            return;

        entity.Status = (int)NotificationDeliveryStatus.Dispatched;
        entity.DispatchedAtUtc = dispatchedAtUtc.ToString("O");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDeliveryEntity ToEntity(NotificationDelivery delivery) => new()
    {
        DeliveryId = delivery.DeliveryId,
        EventId = delivery.EventId,
        Channel = (int)delivery.Channel,
        RecipientUserId = delivery.RecipientUserId,
        PaymentRequestId = delivery.PaymentRequestId,
        EventType = delivery.EventType,
        AmountMinor = delivery.AmountMinor,
        CurrencyCode = delivery.CurrencyCode,
        Title = delivery.Title,
        Body = delivery.Body,
        CreatedAtUtc = delivery.CreatedAtUtc.ToString("O"),
        Status = (int)delivery.Status,
        DispatchedAtUtc = delivery.DispatchedAtUtc?.ToString("O")
    };

    private static NotificationDelivery ToModel(NotificationDeliveryEntity entity) => new(
        entity.DeliveryId,
        entity.EventId,
        (NotificationChannel)entity.Channel,
        entity.RecipientUserId,
        entity.PaymentRequestId,
        entity.EventType,
        entity.AmountMinor,
        entity.CurrencyCode,
        entity.Title,
        entity.Body,
        DateTimeOffset.Parse(entity.CreatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
        (NotificationDeliveryStatus)entity.Status,
        entity.DispatchedAtUtc is null
            ? null
            : DateTimeOffset.Parse(entity.DispatchedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static void EnsureEquivalent(NotificationDelivery existing, NotificationDelivery candidate)
    {
        if (existing.RecipientUserId != candidate.RecipientUserId ||
            existing.PaymentRequestId != candidate.PaymentRequestId ||
            !string.Equals(existing.EventType, candidate.EventType, StringComparison.Ordinal) ||
            existing.AmountMinor != candidate.AmountMinor ||
            !string.Equals(existing.CurrencyCode, candidate.CurrencyCode, StringComparison.Ordinal) ||
            !string.Equals(existing.Title, candidate.Title, StringComparison.Ordinal) ||
            !string.Equals(existing.Body, candidate.Body, StringComparison.Ordinal) ||
            existing.CreatedAtUtc != candidate.CreatedAtUtc)
        {
            throw new InvalidOperationException(
                "Event id and channel are already associated with a different notification delivery.");
        }
    }
}
