using AfriWallet.Disputes.Intelligence.Domain.Findings;
using AfriWallet.Disputes.Intelligence.Domain.Metrics;

namespace AfriWallet.Disputes.Intelligence.Application.Services;

public sealed record CustomerProtectionResult(
    Guid FindingId,
    string SubjectId,
    int Score,
    ProtectionSeverity Severity,
    ProtectionRecommendation Recommendation,
    DisputeIntelligenceMetrics Metrics,
    IReadOnlyCollection<ProtectionPattern> Patterns,
    DateTimeOffset CreatedAtUtc);
