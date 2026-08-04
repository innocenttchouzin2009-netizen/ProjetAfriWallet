namespace Subscriptions.Contracts.Dtos;

public sealed record SubscriptionInvoiceDto(
    string InvoiceId,
    string SubscriptionId,
    DateTimeOffset BillingPeriodStart,
    DateTimeOffset BillingPeriodEnd,
    string Currency,
    long AmountMinor,
    string BillingCycle,
    DateTimeOffset DueAt,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PaidAt,
    int RetryCount,
    int MaxRetries,
    IReadOnlyList<string> Attempts);
