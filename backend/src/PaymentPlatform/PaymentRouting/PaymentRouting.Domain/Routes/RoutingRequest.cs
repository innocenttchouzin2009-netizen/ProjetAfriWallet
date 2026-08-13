namespace PaymentRouting.Domain.Routes;

public sealed record RoutingRequest(
    Guid PaymentIntentId,
    string CountryCode,
    string CurrencyCode,
    long AmountMinor,
    PaymentRail RequestedRail,
    string? PreferredProviderId,
    string CorrelationId);
