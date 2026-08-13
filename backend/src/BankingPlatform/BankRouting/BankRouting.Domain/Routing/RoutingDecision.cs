using BankRouting.Domain.Rails;

namespace BankRouting.Domain.Routing;

public sealed record RoutingDecision(
    Guid DecisionId,
    Guid TransferIntentId,
    string OwnerAwid,
    string IdempotencyKey,
    BankRailType SelectedRail,
    IReadOnlyCollection<BankRailType> FallbackRails,
    string Reason,
    int Score,
    long EstimatedCostMinor,
    DateTime CreatedAtUtc)
{
    public RoutingDecision WithUpdatedReason(string reason) =>
        this with { Reason = reason };
}
