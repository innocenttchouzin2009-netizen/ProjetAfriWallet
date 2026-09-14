using AfriWallet.Notifications.Domain;

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
