using PaymentIntent.Application.Interfaces;
using PaymentIntent.Domain.Methods;
using PaymentIntent.Domain.Money;
using DomainPaymentIntent = PaymentIntent.Domain.Intents.PaymentIntent;

namespace PaymentIntent.Application.Services;

public sealed class PaymentIntentService
{
    private readonly IPaymentIntentRepository _repository;

    public PaymentIntentService(
        IPaymentIntentRepository repository)
    {
        _repository = repository;
    }

    public async Task<DomainPaymentIntent> CreateAsync(
        string reference,
        string payerId,
        string payeeId,
        long amountMinor,
        string currencyCode,
        PaymentMethodType paymentMethod,
        string idempotencyKey,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var existing =
            await _repository
                .GetByIdempotencyKeyAsync(
                    idempotencyKey,
                    cancellationToken);

        if (existing is not null)
            return existing;

        if (payerId == payeeId)
            throw new InvalidOperationException(
                "Payer and payee must differ.");

        if (lifetime <= TimeSpan.Zero ||
            lifetime > TimeSpan.FromHours(24))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "Payment intent lifetime must be between 0 and 24 hours.");
        }

        var intent =
            new DomainPaymentIntent(
                Guid.NewGuid(),
                reference,
                payerId,
                payeeId,
                new MoneyAmount(
                    amountMinor,
                    currencyCode),
                paymentMethod,
                idempotencyKey,
                DateTime.UtcNow.Add(lifetime));

        await _repository.AddAsync(
            intent,
            cancellationToken);

        return intent;
    }

    public async Task<DomainPaymentIntent> AuthorizeAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        var intent =
            await RequireAsync(
                paymentIntentId,
                cancellationToken);

        intent.Authorize();

        return intent;
    }

    public async Task<DomainPaymentIntent> StartProcessingAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        var intent =
            await RequireAsync(
                paymentIntentId,
                cancellationToken);

        intent.StartProcessing();

        return intent;
    }

    public async Task<DomainPaymentIntent> CompleteAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        var intent =
            await RequireAsync(
                paymentIntentId,
                cancellationToken);

        intent.Complete();

        return intent;
    }

    public async Task<DomainPaymentIntent> CancelAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        var intent =
            await RequireAsync(
                paymentIntentId,
                cancellationToken);

        intent.Cancel();

        return intent;
    }

    public Task<DomainPaymentIntent?> GetAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        return _repository.GetAsync(
            paymentIntentId,
            cancellationToken);
    }

    private async Task<DomainPaymentIntent> RequireAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(
                   paymentIntentId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Payment intent not found.");
    }
}
