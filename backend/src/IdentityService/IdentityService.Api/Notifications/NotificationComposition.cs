using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using AfriWallet.P2P.Domain;
using AfriWallet.P2P.Infrastructure;
using AfriWallet.PaymentRequests.Application;
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
        services.AddDbContext<NotificationDeliveryDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IInAppNotificationRepository, EfInAppNotificationRepository>();
        services.AddScoped<INotificationDeliveryRepository, EfNotificationDeliveryRepository>();
        services.AddScoped<INotificationChannelDispatchPort, InAppNotificationChannelDispatchPort>();
        services.AddScoped<INotificationChannelDispatchPort, PushNotificationChannelDispatchPort>();
        services.AddScoped<NotificationRuntimeDeliveryService>();
        services.AddSingleton(NotificationDeliveryRecoveryOptions.Default);
        services.AddScoped<NotificationDeliveryRecoveryService>();
        services.AddScoped<InAppNotificationInboxService>();
        services.AddScoped<NotificationRetentionService>();
        services.AddSingleton(NotificationRetentionOptions.Default);
        services.AddScoped<IPaymentRequestEventTransport, NotificationPaymentRequestEventTransport>();
        return services;
    }
}

public sealed class NotificationPaymentRequestEventTransport(
    PaymentRequestDbContext paymentRequestDbContext,
    IWalletRepository walletRepository,
    IAfWalIdentityDirectory afWalIdentityDirectory,
    IQrRecipientDirectory qrRecipientDirectory,
    NotificationRuntimeDeliveryService runtimeDeliveryService)
    : IPaymentRequestEventTransport
{
    public async Task DispatchAsync(
        PaymentRequestEventDispatch dispatch,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var kind = MapKind(dispatch.EventType);
            var request = await paymentRequestDbContext.PaymentRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == dispatch.PaymentRequestId, cancellationToken);

            if (request is null)
            {
                throw new PaymentRequestEventTransportException(
                    PaymentRequestEventTransportFailureKind.Permanent,
                    $"Payment request {dispatch.PaymentRequestId} was not found for notification delivery.");
            }

            Guid? transferId = kind == AfriWallet.Notifications.Domain.PaymentRequestEventKind.Paid
                ? request.TransferId
                : null;

            var notificationEvent = AfriWallet.Notifications.Domain.PaymentRequestEvent.Create(
                dispatch.EventId,
                dispatch.PaymentRequestId,
                kind,
                dispatch.OccurredAtUtc,
                transferId);

            var audience = new HashSet<Guid>();
            var requesterWallet = await walletRepository.GetAsync(WalletId.From(request.RequesterWalletId), cancellationToken);
            var requesterUserId = requesterWallet?.OwnerId;
            var payerUserId = await ResolvePayerUserIdAsync(request, cancellationToken);

            if (kind == AfriWallet.Notifications.Domain.PaymentRequestEventKind.Created)
            {
                AddAudience(audience, payerUserId);
            }
            else
            {
                AddAudience(audience, requesterUserId);
                AddAudience(audience, payerUserId);
            }

            foreach (var userId in audience)
            {
                await runtimeDeliveryService.DeliverAsync(
                    AfriWallet.Notifications.Domain.InAppNotification.New(userId, notificationEvent),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PaymentRequestEventTransportException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Permanent,
                exception.Message,
                exception);
        }
        catch (Exception exception)
        {
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "In-app notification delivery failed.",
                exception);
        }
    }

    private async Task<Guid?> ResolvePayerUserIdAsync(
        PaymentRequestEntity request,
        CancellationToken cancellationToken)
    {
        if (request.AcceptedPayerWalletId is Guid acceptedPayerWalletId && acceptedPayerWalletId != Guid.Empty)
        {
            var wallet = await walletRepository.GetAsync(WalletId.From(acceptedPayerWalletId), cancellationToken);
            if (wallet is not null && wallet.OwnerId != Guid.Empty)
                return wallet.OwnerId;
        }

        return (RecipientReferenceKind)request.PayerReferenceKind switch
        {
            RecipientReferenceKind.AfWalId => await afWalIdentityDirectory.ResolveOwnerIdAsync(
                request.PayerReferenceValue,
                cancellationToken),
            RecipientReferenceKind.QrToken => await qrRecipientDirectory.ResolveOwnerIdAsync(
                request.PayerReferenceValue,
                cancellationToken),
            _ => throw new ArgumentException("Unsupported payment request payer reference kind.")
        };
    }

    private static AfriWallet.Notifications.Domain.PaymentRequestEventKind MapKind(string eventType) =>
        eventType switch
        {
            "payment-request.created" => AfriWallet.Notifications.Domain.PaymentRequestEventKind.Created,
            "payment-request.accepted" => AfriWallet.Notifications.Domain.PaymentRequestEventKind.Accepted,
            "payment-request.declined" => AfriWallet.Notifications.Domain.PaymentRequestEventKind.Declined,
            "payment-request.cancelled" => AfriWallet.Notifications.Domain.PaymentRequestEventKind.Cancelled,
            "payment-request.expired" => AfriWallet.Notifications.Domain.PaymentRequestEventKind.Expired,
            "payment-request.paid" => AfriWallet.Notifications.Domain.PaymentRequestEventKind.Paid,
            _ => throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Permanent,
                $"Unsupported payment request event type '{eventType}'.")
        };

    private static void AddAudience(HashSet<Guid> audience, Guid? userId)
    {
        if (userId is Guid value && value != Guid.Empty)
            audience.Add(value);
    }
}
