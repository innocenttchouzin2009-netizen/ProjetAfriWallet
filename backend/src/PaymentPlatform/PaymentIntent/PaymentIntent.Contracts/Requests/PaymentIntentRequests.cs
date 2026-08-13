using PaymentIntent.Domain.Methods;

namespace PaymentIntent.Contracts.Requests;

public sealed record CreatePaymentIntentRequest(
    string Reference,
    string PayerId,
    string PayeeId,
    long AmountMinor,
    string CurrencyCode,
    PaymentMethodType PaymentMethod,
    string IdempotencyKey,
    int LifetimeMinutes);
