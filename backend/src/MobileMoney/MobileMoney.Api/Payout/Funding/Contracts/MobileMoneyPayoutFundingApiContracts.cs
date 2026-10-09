using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Contracts;

public sealed record PlanMobileMoneyPayoutFundingRequest(
    Guid CorrelationId,
    long RequiredAmountMinor,
    string CurrencyCode,
    IReadOnlyList<MobileMoneyPayoutFundingAllocationRequest> Allocations,
    DateTimeOffset RequestedAtUtc);

public sealed record MobileMoneyPayoutFundingAllocationRequest(
    string SourceId,
    FundingSourceType SourceType,
    long AmountMinor,
    string CurrencyCode);

public sealed record MobileMoneyPayoutFundingPlanResponse(
    Guid CorrelationId,
    long RequiredAmountMinor,
    string CurrencyCode,
    IReadOnlyList<MobileMoneyPayoutFundingAllocationResponse> Allocations,
    DateTimeOffset PlannedAtUtc);

public sealed record MobileMoneyPayoutFundingAllocationResponse(
    string SourceId,
    FundingSourceType SourceType,
    long AmountMinor,
    string CurrencyCode);
