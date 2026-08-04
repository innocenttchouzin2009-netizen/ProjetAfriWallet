namespace Subscriptions.Contracts.Dtos;

public sealed record SubscriptionConnectorCapabilityDto(
    bool SupportsActivation,
    bool SupportsRenewal,
    bool SupportsSuspension,
    bool SupportsResumption,
    bool SupportsCancellation,
    bool SupportsStatusLookup);

public sealed record SubscriptionConnectorResponseDto(
    string ProviderId,
    string Operation,
    string Status,
    string? CorrelationId,
    string? Message,
    IReadOnlyDictionary<string, string> Payload);

public sealed record SubscriptionConnectorHealthDto(
    string ProviderId,
    string Status,
    string? Message);
