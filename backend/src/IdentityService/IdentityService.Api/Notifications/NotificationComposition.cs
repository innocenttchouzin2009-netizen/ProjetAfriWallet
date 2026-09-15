using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using AfriWallet.P2P.Domain;
using AfriWallet.P2P.Infrastructure;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Notifications;

public static class NotificationComposition
{
    public static IServiceCollection AddInAppNotifications(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Notification database connection string is required.", nameof(connectionString));

        services.AddDbContext<NotificationInboxDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IInAppNotificationRepository, EfInAppNotificationRepository>();
        services.AddScoped<InAppNotificationInboxService>();
        services.AddScoped<NotificationRetentionService>();
        services.AddSingleton(NotificationRetentionOptions.Default);
        services.AddScoped<AfriWallet.Notifications.Application.IPaymentRequestEventPublisher, InAppPaymentRequestEventPublisher>();
        return services;
    }
}

public sealed class InAppPaymentRequestEventPublisher(
    PaymentRequestDbContext paymentRequestDbContext,
    IWalletRepository walletRepository,
    IAfWalIdentityDirectory afWalIdentityDirectory,
    IQrRecipientDirectory qrRecipientDirectory,
    IInAppNotificationRepository notificationRepository)
    : AfriWallet.Notifications.Application.IPaymentRequestEventPublisher
{
    public async Task PublishAsync(
        AfriWallet.Notifications.Domain.PaymentRequestEvent paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();

        var request = await paymentRequestDbContext.PaymentRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == paymentRequestEvent.PaymentRequestId, cancellationToken);
        if (request is null) return;

        var audience = new HashSet<Guid>();
        var requesterWallet = await walletRepository.GetAsync(WalletId.From(request.RequesterWalletId), cancellationToken);
        var requesterUserId = requesterWallet?.OwnerId;

        Guid? payerUserId = (RecipientReferenceKind)request.PayerReferenceKind switch
        {
            RecipientReferenceKind.AfWalId => await afWalIdentityDirectory.ResolveOwnerIdAsync(request.PayerReferenceValue, cancellationToken),
            RecipientReferenceKind.QrToken => await qrRecipientDirectory.ResolveOwnerIdAsync(request.PayerReferenceValue, cancellationToken),
            _ => null
        };

        if (paymentRequestEvent.Kind == AfriWallet.Notifications.Domain.PaymentRequestEventKind.Created)
        {
            if (payerUserId is not null && payerUserId != Guid.Empty) audience.Add(payerUserId.Value);
        }
        else
        {
            if (requesterUserId is not null && requesterUserId != Guid.Empty) audience.Add(requesterUserId.Value);
            if (payerUserId is not null && payerUserId != Guid.Empty) audience.Add(payerUserId.Value);
        }

        foreach (var userId in audience)
        {
            await notificationRepository.AddAsync(
                AfriWallet.Notifications.Domain.InAppNotification.New(userId, paymentRequestEvent),
                cancellationToken);
        }
    }
}
