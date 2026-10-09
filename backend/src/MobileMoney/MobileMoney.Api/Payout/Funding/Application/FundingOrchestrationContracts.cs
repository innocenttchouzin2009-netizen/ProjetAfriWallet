using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed record FundingSourceSnapshot(
    string SourceId,
    FundingSourceType SourceType,
    string CurrencyCode,
    bool IsAvailable,
    long? AvailableMinor);

public sealed record PlanMobileMoneyPayoutFundingCommand(
    Guid CorrelationId,
    long RequiredAmountMinor,
    string CurrencyCode,
    IReadOnlyList<FundingAllocation> Allocations,
    DateTimeOffset RequestedAtUtc);

public sealed record MobileMoneyPayoutFundingPlan(
    Guid CorrelationId,
    long RequiredAmountMinor,
    string CurrencyCode,
    IReadOnlyList<FundingAllocation> Allocations,
    DateTimeOffset PlannedAtUtc);
