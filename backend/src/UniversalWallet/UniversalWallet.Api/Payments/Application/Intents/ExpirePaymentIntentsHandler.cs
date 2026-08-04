using UniversalWallet.Api.Payments.Domain.Intents;

namespace UniversalWallet.Api.Payments.Application.Intents;

public sealed class ExpirePaymentIntentsHandler
{
    private readonly IPaymentIntentRepository _repository;

    public ExpirePaymentIntentsHandler(IPaymentIntentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PaymentIntent>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var intents = await _repository.ListAsync(Guid.Empty, null, cancellationToken);
        var expired = intents.Where(intent => !intent.IsTerminal && intent.ExpiresAt <= DateTimeOffset.UtcNow).ToList();
        foreach (var intent in expired)
        {
            intent.MarkExpired();
            await _repository.AddAsync(intent, cancellationToken);
        }

        return expired;
    }
}
