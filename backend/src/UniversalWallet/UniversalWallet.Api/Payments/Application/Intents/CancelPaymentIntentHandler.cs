using UniversalWallet.Api.Payments.Domain.Intents;

namespace UniversalWallet.Api.Payments.Application.Intents;

public sealed class CancelPaymentIntentHandler
{
    private readonly IPaymentIntentRepository _repository;

    public CancelPaymentIntentHandler(IPaymentIntentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentIntent?> HandleAsync(Guid intentId, CancellationToken cancellationToken = default)
    {
        var intent = await _repository.GetAsync(intentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        if (intent.IsTerminal)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_ALREADY_TERMINAL");
        }

        if (intent.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            intent.MarkExpired();
            await _repository.AddAsync(intent, cancellationToken);
            throw new InvalidOperationException("PAYMENT_INTENT_EXPIRED");
        }

        intent.Cancel();
        await _repository.AddAsync(intent, cancellationToken);
        return intent;
    }
}
