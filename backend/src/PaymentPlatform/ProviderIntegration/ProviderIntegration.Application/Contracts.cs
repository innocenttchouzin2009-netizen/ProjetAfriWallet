namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public sealed record ProviderExecutionRequest(
    string ProviderCode,
    string Operation,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Payload);

public sealed record ProviderExecutionResult(
    bool Success,
    string? ProviderReference,
    string? ErrorCode,
    string? ErrorMessage,
    bool Retryable);

public sealed record ProviderCredential(
    string AccessToken,
    DateTimeOffset ExpiresAt);

public sealed record ProviderWebhookVerificationRequest(
    string ProviderCode,
    string Payload,
    string Signature);

public sealed record ProviderAuditEvent(
    string EventName,
    string ProviderCode,
    string Operation,
    string CorrelationId,
    bool Success,
    DateTimeOffset OccurredAt);

public sealed record ProviderTelemetryEvent(
    string Metric,
    string ProviderCode,
    string Outcome,
    double DurationMs,
    DateTimeOffset OccurredAt);