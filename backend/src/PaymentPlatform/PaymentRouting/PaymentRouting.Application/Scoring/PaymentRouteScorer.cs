using PaymentRouting.Domain.Policies;
using PaymentRouting.Domain.Providers;
using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Application.Scoring;

public sealed class PaymentRouteScorer
{
    public PaymentRoute Score(
        PaymentProvider provider,
        RoutingPolicy policy,
        bool isFallback)
    {
        var normalizedCost =
            Math.Max(
                0m,
                100m - provider.BaseCostScore);

        var reliability =
            (decimal)(provider.SuccessRate * 100d);

        var latency =
            Math.Max(
                0m,
                100m -
                (decimal)Math.Min(
                    provider.AverageLatencyMs / 10d,
                    100d));

        var priority =
            Math.Max(
                0m,
                100m - provider.Priority);

        var score =
            normalizedCost * policy.CostWeight
            + reliability * policy.ReliabilityWeight
            + latency * policy.LatencyWeight
            + priority * policy.PriorityWeight;

        return new PaymentRoute(
            provider.ProviderId,
            provider.Rail,
            decimal.Round(score, 4),
            provider.BaseCostScore,
            provider.SuccessRate,
            provider.AverageLatencyMs,
            provider.Priority,
            isFallback);
    }
}
