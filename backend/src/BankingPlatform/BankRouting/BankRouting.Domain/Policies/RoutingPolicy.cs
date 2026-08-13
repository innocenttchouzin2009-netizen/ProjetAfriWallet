namespace BankRouting.Domain.Policies;

public sealed record RoutingPolicy(
    int PreferredPriorityBoost,
    int HealthyRailBoost,
    int CostSensitivityWeight,
    int MaxFallbackCount)
{
    public static readonly RoutingPolicy Default = new(
        PreferredPriorityBoost: 25,
        HealthyRailBoost: 10,
        CostSensitivityWeight: 2,
        MaxFallbackCount: 3);
}
