using AfriWallet.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace AfriWallet.Notifications.Application;

public interface IPaymentRequestEventPublisher
{
    Task PublishAsync(PaymentRequestEvent paymentRequestEvent, CancellationToken cancellationToken = default);
}

public sealed class PaymentRequestEventDispatcher(IPaymentRequestEventPublisher publisher)
{
    public Task PublishAsync(
        PaymentRequestEvent paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();
        return publisher.PublishAsync(paymentRequestEvent, cancellationToken);
    }
}

public sealed class LoggingPaymentRequestEventPublisher(
    ILogger<LoggingPaymentRequestEventPublisher> logger) : IPaymentRequestEventPublisher
{
    public Task PublishAsync(
        PaymentRequestEvent paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "PaymentRequestEvent emitted: EventId={EventId} PaymentRequestId={PaymentRequestId} Kind={Kind} OccurredAtUtc={OccurredAtUtc} TransferId={TransferId}",
            paymentRequestEvent.EventId,
            paymentRequestEvent.PaymentRequestId,
            paymentRequestEvent.Kind,
            paymentRequestEvent.OccurredAtUtc,
            paymentRequestEvent.TransferId);

        return Task.CompletedTask;
    }
}
