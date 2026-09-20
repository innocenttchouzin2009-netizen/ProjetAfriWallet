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

    public async Task<IReadOnlyList<NotificationDelivery>> ClaimRecoverableAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset leaseUntilUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (nowUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Recovery timestamp must be UTC.", nameof(nowUtc));
        if (leaseUntilUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Recovery lease timestamp must be UTC.", nameof(leaseUntilUtc));
        if (leaseUntilUtc <= nowUtc) throw new ArgumentException("Recovery lease must expire after now.", nameof(leaseUntilUtc));
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));

        var now = Format(nowUtc);
        var leaseUntil = Format(leaseUntilUtc);
        var candidates = await dbContext.Deliveries
            .AsNoTracking()
            .Where(x =>
                x.Status == (int)NotificationDeliveryStatus.Pending &&
                (x.RecoveryLeaseUntilUtc == null || string.Compare(x.RecoveryLeaseUntilUtc, now) <= 0))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.DeliveryId)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        var claimed = new List<NotificationDelivery>(candidates.Length);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var updated = await dbContext.Deliveries
                .Where(x =>
                    x.DeliveryId == candidate.DeliveryId &&
                    x.Status == (int)NotificationDeliveryStatus.Pending &&
                    (x.RecoveryLeaseUntilUtc == null || string.Compare(x.RecoveryLeaseUntilUtc, now) <= 0))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.RecoveryLeaseUntilUtc, leaseUntil),
                    cancellationToken);

            if (updated == 1)
                claimed.Add(Restore(candidate));
        }

        return claimed;
    }

    public async Task ReleaseRecoveryClaimAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (deliveryId == Guid.Empty) throw new ArgumentException("Delivery id cannot be empty.", nameof(deliveryId));

        await dbContext.Deliveries
            .Where(x =>
                x.DeliveryId == deliveryId &&
                x.Status == (int)NotificationDeliveryStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RecoveryLeaseUntilUtc, (string?)null),
                cancellationToken);
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
        entity.RecoveryLeaseUntilUtc = null;
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
