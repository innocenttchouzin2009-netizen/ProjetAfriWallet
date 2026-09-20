using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestLifecycleMutationStore
{
    Task AddAsync(
        PaymentRequest request,
        PaymentRequestLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PaymentRequest request,
        PaymentRequestLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default);
}
