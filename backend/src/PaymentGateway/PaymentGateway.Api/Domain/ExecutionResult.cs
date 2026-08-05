namespace PaymentGateway.Api.Domain;

public sealed record ExecutionResult(
    Guid ExecutionId,
    ExecutionStatus Status,
    string ConnectorType,
    string ProviderCode,
    string ProviderReference,
    int RetryCount,
    long DurationMs,
    string? FailureReason,
    DateTimeOffset UpdatedAt);
