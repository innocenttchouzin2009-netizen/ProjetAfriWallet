using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Contracts.Requests;

public sealed record RoutePaymentRequest(
    Guid PaymentIntentId,
    string CountryCode,
    string CurrencyCode,
    long AmountMinor,
    PaymentRail RequestedRail,
    string? PreferredProviderId,
    string CorrelationId);
