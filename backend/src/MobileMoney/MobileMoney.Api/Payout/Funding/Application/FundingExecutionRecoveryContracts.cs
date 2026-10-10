using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed record RecoverMobileMoneyPayoutFundingCommand(
    Guid CorrelationId);

public sealed record FundingRecoveryProviderRequest(
    Guid AttemptId,
    Guid CorrelationId,
    string SourceId,
    FundingSourceType SourceType,
    long AmountMinor,
    string CurrencyCode,
    string IdempotencyKey,
    DateTimeOffset ProcessingStartedAtUtc);

public enum FundingRecoveryDisposition
{
    StillProcessing = 0,
    Succeeded = 1,
    Failed = 2
}

public sealed record FundingRecoveryProviderResult(
    FundingRecoveryDisposition Disposition,
    string? ProviderReference,
    string? FailureCode);

public sealed record MobileMoneyPayoutFundingRecoveryResult(
    Guid CorrelationId,
    IReadOnlyList<FundingAttempt> Attempts);
