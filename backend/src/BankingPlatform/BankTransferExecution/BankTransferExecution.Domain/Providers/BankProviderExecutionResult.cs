namespace AfriWallet.BankingPlatform.BankTransferExecution.Domain.Providers;

public sealed record BankProviderExecutionResult(
    bool Success,
    string? ProviderReference,
    string? ErrorCode,
    bool Retryable);
