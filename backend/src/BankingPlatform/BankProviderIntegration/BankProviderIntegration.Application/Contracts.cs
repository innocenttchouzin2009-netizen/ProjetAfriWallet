namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application;

public sealed record SubmitProviderTransferRequest(
    Guid ExecutionId,
    string ProviderCode,
    string RailCode,
    long AmountMinor,
    string CurrencyCode,
    string IdempotencyKey);

public sealed record ProviderSubmission(
    bool Success,
    string? ProviderReference,
    string? ErrorCode,
    bool Retryable);

public sealed record ProviderWebhookRequest(
    string ProviderCode,
    string Payload,
    string Signature);

public sealed record ProviderWebhookResult(
    bool Accepted,
    string ProviderCode,
    string EventType);
