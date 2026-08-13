using System.Collections.Concurrent;
using PaymentIntent.Application.Interfaces;
using DomainPaymentIntent = PaymentIntent.Domain.Intents.PaymentIntent;

namespace PaymentIntent.Infrastructure.Repositories;

public sealed class InMemoryPaymentIntentRepository :
    IPaymentIntentRepository
{
    private readonly ConcurrentDictionary<
        Guid,
        DomainPaymentIntent> _items = new();

    public Task AddAsync(
        DomainPaymentIntent paymentIntent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_items.Values.Any(x =>
                string.Equals(
                    x.IdempotencyKey,
                    paymentIntent.IdempotencyKey,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Payment intent idempotency key already exists.");
        }

        if (!_items.TryAdd(
                paymentIntent.PaymentIntentId,
                paymentIntent))
        {
            throw new InvalidOperationException(
                "Payment intent already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<DomainPaymentIntent?> GetAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _items.TryGetValue(
            paymentIntentId,
            out var paymentIntent);

        return Task.FromResult(paymentIntent);
    }

    public Task<DomainPaymentIntent?>
        GetByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var paymentIntent =
            _items.Values.FirstOrDefault(x =>
                string.Equals(
                    x.IdempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal));

        return Task.FromResult(paymentIntent);
    }
}
