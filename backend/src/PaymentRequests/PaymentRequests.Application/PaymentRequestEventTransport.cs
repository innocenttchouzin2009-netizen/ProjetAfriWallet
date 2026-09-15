namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestEventTransportFailureKind
{
    Transient = 1,
    Permanent = 2
}

public sealed record PaymentRequestEventDispatch(
    Guid EventId,
    Guid PaymentRequestId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);

public sealed class PaymentRequestEventTransportException : Exception
{
    public PaymentRequestEventTransportException(
        PaymentRequestEventTransportFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public PaymentRequestEventTransportFailureKind FailureKind { get; }
}

public interface IPaymentRequestEventTransport
{
    Task DispatchAsync(
        PaymentRequestEventDispatch dispatch,
        CancellationToken cancellationToken = default);
}

public enum PaymentRequestEventDeliveryFailureKind
{
    Transient = 1,
    Permanent = 2
}

public sealed class PaymentRequestEventDeliveryException : Exception
{
    public PaymentRequestEventDeliveryException(
        PaymentRequestEventDeliveryFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public PaymentRequestEventDeliveryFailureKind FailureKind { get; }
}

public sealed class ProviderNeutralPaymentRequestEventDeliveryAdapter(IPaymentRequestEventTransport transport)
    : IPaymentRequestEventDeliveryPort
{
    public async Task DeliverAsync(
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

        if (string.IsNullOrWhiteSpace(paymentRequestEvent.EventType))
        {
            throw new ArgumentException("Event type is required.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamp must be UTC.", nameof(paymentRequestEvent));
        }

        try
        {
            await transport.DispatchAsync(
                new PaymentRequestEventDispatch(
                    paymentRequestEvent.EventId,
                    paymentRequestEvent.PaymentRequestId.Value,
                    paymentRequestEvent.EventType,
                    paymentRequestEvent.OccurredAtUtc,
                    paymentRequestEvent.PayloadJson),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PaymentRequestEventTransportException exception)
        {
            var failureKind = exception.FailureKind switch
            {
                PaymentRequestEventTransportFailureKind.Transient => PaymentRequestEventDeliveryFailureKind.Transient,
                PaymentRequestEventTransportFailureKind.Permanent => PaymentRequestEventDeliveryFailureKind.Permanent,
                _ => throw new ArgumentOutOfRangeException(nameof(exception.FailureKind))
            };

            throw new PaymentRequestEventDeliveryException(failureKind, exception.Message, exception);
        }
    }
}
