using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using AfriWallet.P2P.Domain;
using AfriWallet.P2P.Infrastructure;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Api.Notifications;

public static class NotificationComposition
{
    public static IServiceCollection AddInAppNotifications(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Notification database connection string is required.", nameof(connectionString));

        services.AddDbContext<NotificationInboxDbContext>(options => options.UseSqlite(connectionString));
        services.AddDbContext<PushEventDeliveryDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IInAppNotificationRepository, EfInAppNotificationRepository>();
        services.AddScoped<IPushEventDeliveryRepository, EfPushEventDeliveryRepository>();
        services.AddScoped<InAppNotificationInboxService>();
        services.AddScoped<NotificationRetentionService>();
        services.AddSingleton(NotificationRetentionOptions.Default);
        services.AddSingleton(PushEventRetryPolicy.Default);
        services.AddScoped<PushDeliveryOrchestrationService>();
        services.AddScoped<NotificationEventPushDeliveryService>();
        services.AddScoped<INotificationPushDispatchPort, RuntimeNotificationPushDispatchPort>();
        services.AddScoped<NotificationDeliveryDispatchService>();
        services.AddScoped<IPaymentRequestEventTransport, InAppPaymentRequestEventTransport>();
        return services;
    }
}

public sealed class RuntimeNotificationPushDispatchPort(IServiceProvider serviceProvider)
    : INotificationPushDispatchPort
{
    public async Task<PushEventDeliveryResult> DispatchAsync(
        AfriWallet.Notifications.Domain.InAppNotification notification,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        // External provider delivery remains optional at runtime. When a concrete
        // IPushDeliveryPort is configured, every attempt flows through the certified
        // NotificationEventPushDeliveryService and its preference-aware router.
        if (serviceProvider.GetService<IPushDeliveryPort>() is null)
        {
            return new PushEventDeliveryResult(
                notification.EventId,
                AttemptedTargets: 0,
                Delivered: 0,
                RetryScheduled: 0,
                TerminalFailures: 0);
        }

        var deliveryService = serviceProvider.GetRequiredService<NotificationEventPushDeliveryService>();
        return await deliveryService.DeliverAsync(notification, attemptedAtUtc, cancellationToken);
    }
}

public sealed class InAppPaymentRequestEventTransport(
    PaymentRequestDbContext paymentRequestDbContext,
    IWalletRepository walletRepository,
    IAfWalIdentityDirectory afWalIdentityDirectory,
    IQrRecipientDirectory qrRecipientDirectory,
    NotificationDeliveryDispatchService dispatchService)
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
                var notification = AfriWallet.Notifications.Domain.InAppNotification.New(userId, notificationEvent);
                await dispatchService.DispatchAsync(notification, dispatch.OccurredAtUtc, cancellationToken);
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
                "Notification delivery failed.",
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
