namespace Subscriptions.Contracts.Dtos;

public sealed record UserSubscriptionDto(
    string SubscriptionId,
    string UserId,
    string ProviderId,
    string PlanId,
    string OfferId,
    string Currency,
    long AmountMinor,
    string BillingCycle,
    int GracePeriodDays,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset? RenewalAt,
    DateTimeOffset? LastPaymentAt,
    IReadOnlyList<string> History);
