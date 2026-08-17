namespace AfriWallet.Fraud.Intelligence.Domain.Patterns;

public sealed record FraudPattern
{
    public FraudPattern(Guid patternId, FraudPatternType type, int score, string reason, IReadOnlyCollection<string> entityIds, DateTimeOffset detectedAtUtc)
    {
        if (patternId == Guid.Empty) throw new ArgumentException("Pattern id is required.");
        if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Pattern reason is required.");
        if (entityIds is null || entityIds.Count == 0) throw new ArgumentException("Pattern entities are required.");
        PatternId = patternId;
        Type = type;
        Score = score;
        Reason = reason.Trim();
        EntityIds = entityIds;
        DetectedAtUtc = detectedAtUtc;
    }

    public Guid PatternId { get; }
    public FraudPatternType Type { get; }
    public int Score { get; }
    public string Reason { get; }
    public IReadOnlyCollection<string> EntityIds { get; }
    public DateTimeOffset DetectedAtUtc { get; }
}