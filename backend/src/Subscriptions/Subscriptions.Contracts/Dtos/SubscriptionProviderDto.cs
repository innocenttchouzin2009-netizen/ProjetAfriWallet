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

public sealed record SubscriptionCatalogOfferDto(
    string OfferId,
    string ProviderId,
    string Name,
    string Description,
    string Category,
    string Country,
    string Currency,
    long PriceMinor,
    string BillingCycle,
    bool IsFeatured,
    bool IsAvailable,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    string? PromotionCode,
    int? DiscountPercent,
    bool IsNew,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
