using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed record ExecuteMobileMoneyPayoutFundingCommand(
    Guid CorrelationId);

public sealed record FundingExecutionProviderRequest(
    Guid AttemptId,
    Guid CorrelationId,
    string SourceId,
    FundingSourceType SourceType,
    long AmountMinor,
    string CurrencyCode,
    string IdempotencyKey,
    DateTimeOffset RequestedAtUtc);

public sealed record FundingExecutionProviderResult(
    bool Succeeded,
    string? ProviderReference,
    string? FailureCode);

public sealed record MobileMoneyPayoutFundingExecutionResult(
    Guid CorrelationId,
    IReadOnlyList<FundingAttempt> Attempts);
