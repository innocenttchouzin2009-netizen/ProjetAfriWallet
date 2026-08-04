namespace Subscriptions.Contracts.Dtos;

public sealed record SubscriptionProviderDto(
    string ProviderId,
    string Code,
    string DisplayName,
    string Description,
    string Category,
    string? LogoUrl,
    string? Website,
    IReadOnlyList<string> SupportedCountries,
    IReadOnlyList<string> SupportedCurrencies,
    string IntegrationType,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);

public sealed record SubscriptionPlanDto(
    string PlanId,
    string ProviderId,
    string Name,
    string Description,
    string BillingCycle,
    string Currency,
    long AmountMinor,
    string Country,
    string Status);
