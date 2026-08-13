using DomainPaymentIntent = PaymentIntent.Domain.Intents.PaymentIntent;

namespace PaymentIntent.Application.Interfaces;

public interface IPaymentIntentRepository
{
    Task AddAsync(
        DomainPaymentIntent paymentIntent,
        CancellationToken cancellationToken);

    Task<DomainPaymentIntent?> GetAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken);

    Task<DomainPaymentIntent?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);
}
