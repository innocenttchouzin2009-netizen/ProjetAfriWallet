namespace BankRouting.Application.Contracts;

public sealed record RoutingRequest(
    Guid TransferIntentId,
    string OwnerAwid,
    string CountryCode,
    string CurrencyCode,
    long AmountMinor,
    string IdempotencyKey,
    string? PreferredRail = null);
