using AfriWallet.P2P.Domain;

namespace AfriWallet.PaymentRequests.Webhooks;

public interface IPaymentRequestDeliveryRecipientResolver
{
    Task<RecipientReference?> ResolveAsync(Guid paymentRequestId, CancellationToken cancellationToken = default);
}

public sealed class PaymentRequestDeliveryDestinationSelector(
    IPaymentRequestDeliveryRecipientResolver recipientResolver,
    IPaymentRequestDeliverySubscriptionRegistry registry)
{
    public async Task<IReadOnlyList<PaymentRequestDeliverySubscription>> SelectAsync(
        Guid paymentRequestId, string eventType, CancellationToken cancellationToken = default)
    {
        if (paymentRequestId == Guid.Empty)
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestId));
        var normalizedEventType = PaymentRequestDeliverySubscription.NormalizeEventType(eventType);
        var recipient = await recipientResolver.ResolveAsync(paymentRequestId, cancellationToken);
        if (recipient is null) return Array.Empty<PaymentRequestDeliverySubscription>();
        return await registry.ListActiveAsync(
            PaymentRequestDeliveryRecipientBinding.From(recipient),
            normalizedEventType,
            cancellationToken);
    }
}
