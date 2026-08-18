using AfriWallet.Disputes.Intelligence.Domain.Metrics;

namespace AfriWallet.Disputes.Intelligence.Domain.Findings;

public sealed class ProtectionFinding
{
    public ProtectionFinding(
        Guid findingId,
        string subjectId,
        int score,
        ProtectionSeverity severity,
        ProtectionRecommendation recommendation,
        DisputeIntelligenceMetrics metrics,
        IReadOnlyCollection<ProtectionPattern> patterns,
        DateTimeOffset createdAtUtc)
    {
        if (findingId == Guid.Empty)
            throw new ArgumentException("Finding id is required.", nameof(findingId));
        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException("Subject id is required.", nameof(subjectId));
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(patterns);

        FindingId = findingId;
        SubjectId = subjectId.Trim();
        Score = Math.Clamp(score, 0, 100);
        Severity = severity;
        Recommendation = recommendation;
        Metrics = metrics;
        Patterns = patterns;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid FindingId { get; }
    public string SubjectId { get; }
    public int Score { get; }
    public ProtectionSeverity Severity { get; }
    public ProtectionRecommendation Recommendation { get; }
    public DisputeIntelligenceMetrics Metrics { get; }
    public IReadOnlyCollection<ProtectionPattern> Patterns { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}
