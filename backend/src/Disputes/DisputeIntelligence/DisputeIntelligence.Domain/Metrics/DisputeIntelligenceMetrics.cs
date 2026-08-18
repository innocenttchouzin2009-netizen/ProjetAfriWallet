namespace AfriWallet.Disputes.Intelligence.Domain.Metrics;

public sealed record DisputeIntelligenceMetrics(
    int ClaimCount,
    int EligibleClaimCount,
    int FavorableDecisionCount,
    int RefundRecommendationCount,
    int ChargebackRecommendationCount,
    int FailedResolutionCount,
    int RepeatedMerchantCount,
    double AverageResolutionHours);
