namespace PaymentRouting.Domain.Policies;

public sealed record RoutingPolicy(
    decimal CostWeight,
    decimal ReliabilityWeight,
    decimal LatencyWeight,
    decimal PriorityWeight)
{
    public static RoutingPolicy Default =>
        new(
            CostWeight: 0.25m,
            ReliabilityWeight: 0.40m,
            LatencyWeight: 0.20m,
            PriorityWeight: 0.15m);
}
