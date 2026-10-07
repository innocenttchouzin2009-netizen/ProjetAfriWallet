using AfriWallet.Transfer.Domain.Funding;

namespace AfriWallet.Transfer.Application.Funding;

public sealed record FundingSourceSnapshot(
    string SourceId,
    FundingSourceType SourceType,
    string CurrencyCode,
    bool IsAvailable,
    long? AvailableMinor);

public sealed record PlanTransferFundingCommand(
    Guid CorrelationId,
    long RequiredAmountMinor,
    string CurrencyCode,
    IReadOnlyList<FundingAllocation> Allocations,
    DateTimeOffset RequestedAtUtc);

public sealed record TransferFundingPlan(
    Guid CorrelationId,
    long RequiredAmountMinor,
    string CurrencyCode,
    IReadOnlyList<FundingAllocation> Allocations,
    DateTimeOffset PlannedAtUtc);
