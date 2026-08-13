namespace AfriWallet.BankingPlatform.BankTransferExecution.Application;

public sealed record ExecuteBankTransferRequest(
    Guid TransferIntentId,
    Guid RoutingDecisionId,
    string ProviderCode,
    string RailCode,
    long AmountMinor,
    string CurrencyCode,
    string IdempotencyKey);

public sealed record BankTransferExecutionView(
    Guid ExecutionId,
    Guid TransferIntentId,
    Guid RoutingDecisionId,
    string ProviderCode,
    string RailCode,
    long AmountMinor,
    string CurrencyCode,
    string Status,
    string? ProviderReference,
    string? FailureCode);
