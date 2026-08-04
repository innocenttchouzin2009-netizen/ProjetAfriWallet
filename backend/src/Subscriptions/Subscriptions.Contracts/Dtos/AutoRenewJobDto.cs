namespace Subscriptions.Contracts.Dtos;

public sealed record AutoRenewJobDto(
    string JobId,
    string SubscriptionId,
    DateTimeOffset ScheduledFor,
    string Status,
    int RetryCount,
    int MaxRetries,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
