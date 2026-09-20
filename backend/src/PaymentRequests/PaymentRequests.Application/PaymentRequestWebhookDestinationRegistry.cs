using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestWebhookDestinationRegistry
{
    Task<PaymentRequestWebhookDestination?> GetAsync(
        PaymentRequestWebhookDestinationId destinationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentRequestWebhookDestination>> ListByRecipientAsync(
        PaymentRequestWebhookRecipientId recipientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentRequestWebhookDestination>> ResolveActiveForEventAsync(
        PaymentRequestWebhookRecipientId recipientId,
        PaymentRequestLifecycleEventKind eventKind,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PaymentRequestWebhookDestination destination,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PaymentRequestWebhookDestination destination,
        CancellationToken cancellationToken = default);
}
