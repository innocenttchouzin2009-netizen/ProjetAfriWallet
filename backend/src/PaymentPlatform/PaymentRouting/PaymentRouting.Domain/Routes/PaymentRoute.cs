namespace PaymentRouting.Domain.Routes;

public sealed record PaymentRoute(
    string ProviderId,
    PaymentRail Rail,
    decimal Score,
    decimal CostScore,
    double SuccessRate,
    double AverageLatencyMs,
    int Priority,
    bool IsFallback);
