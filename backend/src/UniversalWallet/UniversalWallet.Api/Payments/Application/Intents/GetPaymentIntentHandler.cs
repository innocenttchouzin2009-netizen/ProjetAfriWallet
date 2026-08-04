using UniversalWallet.Api.Payments.Domain.Intents;

namespace UniversalWallet.Api.Payments.Application.Intents;

public sealed record GetPaymentIntentResponse(
    Guid IntentId,
    PaymentIntentStatus Status,
    Guid SourceWalletId,
    RecipientResponse Recipient,
    long AmountMinor,
    string CurrencyCode,
    PaymentPurpose Purpose,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed class GetPaymentIntentHandler
{
    private readonly IPaymentIntentRepository _repository;

    public GetPaymentIntentHandler(IPaymentIntentRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPaymentIntentResponse?> HandleAsync(Guid intentId, CancellationToken cancellationToken = default)
    {
        var intent = await _repository.GetAsync(intentId, cancellationToken);
        return intent is null ? null : ToResponse(intent);
    }

    private static GetPaymentIntentResponse ToResponse(PaymentIntent intent)
    {
        return new GetPaymentIntentResponse(
            intent.Id,
            intent.Status,
            intent.SourceWalletId,
            new RecipientResponse(intent.RecipientType, intent.RecipientReference),
            intent.AmountMinor,
            intent.CurrencyCode,
            intent.Purpose,
            intent.ExpiresAt,
            intent.CreatedAt);
    }
}
