using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Notification.Persistence;

public sealed class EfPaymentRequestNotificationProjectionStore(PaymentRequestNotificationDbContext db)
    : IPaymentRequestNotificationProjectionStore
{
    public async Task<bool> TryAddAsync(
        PaymentRequestNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        var exists = await db.Notifications
            .AsNoTracking()
            .AnyAsync(
                x => x.SourceEventId == notification.SourceEventId &&
                     x.RecipientOwnerId == notification.RecipientOwnerId &&
                     x.Audience == (int)notification.Audience,
                cancellationToken);

        if (exists)
        {
            return false;
        }

        var entity = ToEntity(notification);
        db.Notifications.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            var duplicate = await db.Notifications
                .AsNoTracking()
                .AnyAsync(
                    x => x.SourceEventId == notification.SourceEventId &&
                         x.RecipientOwnerId == notification.RecipientOwnerId &&
                         x.Audience == (int)notification.Audience,
                    cancellationToken);

            if (duplicate)
            {
                return false;
            }

            throw;
        }
    }

    public async Task<PaymentRequestNotification?> GetAsync(
        PaymentRequestNotificationId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await db.Notifications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpdateAsync(
        PaymentRequestNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await db.Notifications.SingleOrDefaultAsync(x => x.Id == notification.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException("Payment request notification was not found.");

        entity.ReadStatus = (int)notification.ReadStatus;
        entity.ReadAtUtc = notification.ReadAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static PaymentRequestNotificationEntity ToEntity(PaymentRequestNotification notification) => new()
    {
        Id = notification.Id.Value,
        SourceEventId = notification.SourceEventId,
        PaymentRequestId = notification.PaymentRequestId.Value,
        Kind = (int)notification.Kind,
        Audience = (int)notification.Audience,
        RecipientOwnerId = notification.RecipientOwnerId,
        RequesterWalletId = notification.RequesterWalletId.Value,
        CurrencyCode = notification.Currency.Code,
        AmountMinor = notification.AmountMinor,
        OccurredAtUtc = notification.OccurredAtUtc,
        RequestStatus = (int)notification.RequestStatus,
        ExpiresAtUtc = notification.ExpiresAtUtc,
        TransferId = notification.TransferId,
        ReadStatus = (int)notification.ReadStatus,
        ReadAtUtc = notification.ReadAtUtc
    };

    private static PaymentRequestNotification ToDomain(PaymentRequestNotificationEntity entity) =>
        PaymentRequestNotification.Restore(
            PaymentRequestNotificationId.From(entity.Id),
            entity.SourceEventId,
            PaymentRequestId.From(entity.PaymentRequestId),
            (PaymentRequestNotificationKind)entity.Kind,
            (PaymentRequestNotificationAudience)entity.Audience,
            entity.RecipientOwnerId,
            WalletId.From(entity.RequesterWalletId),
            Currency.Create(entity.CurrencyCode),
            entity.AmountMinor,
            entity.OccurredAtUtc,
            (PaymentRequestStatus)entity.RequestStatus,
            entity.ExpiresAtUtc,
            entity.TransferId,
            (PaymentRequestNotificationReadStatus)entity.ReadStatus,
            entity.ReadAtUtc);
}
