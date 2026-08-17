using AfriWallet.Fraud.Intelligence.Domain.Patterns;

namespace AfriWallet.Fraud.Intelligence.Domain.Findings;

public sealed record IntelligenceFinding
{
    public IntelligenceFinding(Guid findingId, string subjectAwid, int correlationScore, IntelligenceSeverity severity, IReadOnlyCollection<FraudPattern> patterns, DateTimeOffset createdAtUtc)
    {
        if (findingId == Guid.Empty) throw new ArgumentException("Finding id is required.");
        if (string.IsNullOrWhiteSpace(subjectAwid)) throw new ArgumentException("Subject AWID is required.");
        if (correlationScore is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(correlationScore));
        if (patterns is null) throw new ArgumentNullException(nameof(patterns));
        FindingId = findingId;
        SubjectAwid = subjectAwid.Trim();
        CorrelationScore = correlationScore;
        Severity = severity;
        Patterns = patterns;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid FindingId { get; }
    public string SubjectAwid { get; }
    public int CorrelationScore { get; }
    public IntelligenceSeverity Severity { get; }
    public IReadOnlyCollection<FraudPattern> Patterns { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}