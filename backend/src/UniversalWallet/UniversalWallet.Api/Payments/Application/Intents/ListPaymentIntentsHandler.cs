using UniversalWallet.Api.Payments.Domain.Intents;

namespace UniversalWallet.Api.Payments.Application.Intents;

public sealed class ListPaymentIntentsHandler
{
    private readonly IPaymentIntentRepository _repository;

    public ListPaymentIntentsHandler(IPaymentIntentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PaymentIntent>> HandleAsync(Guid payerAwid, PaymentIntentStatus? status, CancellationToken cancellationToken = default)
    {
        return await _repository.ListAsync(payerAwid, status, cancellationToken);
    }
}
