using AfriWallet.PaymentRequests.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Notifications.Application;

public sealed class PaymentRequestNotificationRecipientResolver(
    IPaymentRequestRepository paymentRequestRepository,
    IPaymentRequestRecipientResolver payerResolver,
    IWalletRepository walletRepository)
    : IPaymentRequestNotificationRecipientResolver
{
    public async Task<PaymentRequestNotificationContext> ResolveAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();

        var request = await paymentRequestRepository.GetAsync(
            paymentRequestEvent.PaymentRequestId,
            cancellationToken)
            ?? throw new InvalidOperationException("Payment request was not found for notification delivery.");

        var targetPayer = paymentRequestEvent.EventType switch
        {
            "payment-request.created" => true,
            "payment-request.cancelled" => true,
            "payment-request.accepted" => false,
            "payment-request.declined" => false,
            "payment-request.expired" => false,
            "payment-request.paid" => false,
            _ => throw new NotSupportedException($"Unsupported payment request event type '{paymentRequestEvent.EventType}'.")
        };

        WalletId recipientWalletId;
        if (targetPayer)
        {
            if (request.AcceptedPayerWalletId is { } acceptedPayerWalletId)
            {
                recipientWalletId = acceptedPayerWalletId;
            }
            else
            {
                recipientWalletId = await payerResolver.ResolveAsync(
                    request.PayerReference,
                    request.Currency,
                    cancellationToken)
                    ?? throw new InvalidOperationException("Payment request payer could not be resolved for notification delivery.");
            }
        }
        else
        {
            recipientWalletId = request.RequesterWalletId;
        }

        var wallet = await walletRepository.GetAsync(recipientWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Notification recipient wallet was not found.");

        if (wallet.OwnerId == Guid.Empty)
        {
            throw new InvalidOperationException("Notification recipient owner id is invalid.");
        }

        return new PaymentRequestNotificationContext(
            wallet.OwnerId,
            request.AmountMinor,
            request.Currency.Code);
    }
}

public sealed class PaymentRequestEventNotificationConsumer(
    IPaymentRequestNotificationRecipientResolver recipientResolver,
    INotificationDeliveryPort notificationDeliveryPort)
{
    public async Task ConsumeAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();

        if (paymentRequestEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.PaymentRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamp must be UTC.", nameof(paymentRequestEvent));
        }

        var context = await recipientResolver.ResolveAsync(paymentRequestEvent, cancellationToken);
        var (title, body) = MapContent(paymentRequestEvent.EventType, paymentRequestEvent.PaymentRequestId.Value);

        var notification = new InAppNotification(
            paymentRequestEvent.EventId,
            paymentRequestEvent.EventId,
            context.RecipientUserId,
            paymentRequestEvent.PaymentRequestId.Value,
            paymentRequestEvent.EventType,
            context.AmountMinor,
            context.CurrencyCode,
            title,
            body,
            paymentRequestEvent.OccurredAtUtc,
            NotificationChannel.InApp);

        await notificationDeliveryPort.DeliverAsync(notification, cancellationToken);
    }

    private static (string Title, string Body) MapContent(string eventType, Guid paymentRequestId) =>
        eventType switch
        {
            "payment-request.created" => ("Payment request received", $"A new payment request {paymentRequestId} requires your attention."),
            "payment-request.accepted" => ("Payment request accepted", $"Payment request {paymentRequestId} was accepted."),
            "payment-request.declined" => ("Payment request declined", $"Payment request {paymentRequestId} was declined."),
            "payment-request.cancelled" => ("Payment request cancelled", $"Payment request {paymentRequestId} was cancelled."),
            "payment-request.expired" => ("Payment request expired", $"Payment request {paymentRequestId} expired."),
            "payment-request.paid" => ("Payment received", $"Payment request {paymentRequestId} was paid successfully."),
            _ => throw new NotSupportedException($"Unsupported payment request event type '{eventType}'.")
        };
}

public sealed class InAppPaymentRequestEventTransport(PaymentRequestEventNotificationConsumer consumer)
    : IPaymentRequestEventTransport
{
    public Task DispatchAsync(
        PaymentRequestEventDispatch dispatch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        var envelope = new PaymentRequestEventEnvelope(
            dispatch.EventId,
            AfriWallet.PaymentRequests.Domain.PaymentRequestId.From(dispatch.PaymentRequestId),
            dispatch.EventType,
            dispatch.OccurredAtUtc,
            dispatch.PayloadJson);

        return consumer.ConsumeAsync(envelope, cancellationToken);
    }
}
