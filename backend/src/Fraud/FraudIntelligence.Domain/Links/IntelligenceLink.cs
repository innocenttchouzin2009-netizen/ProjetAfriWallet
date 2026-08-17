namespace AfriWallet.Fraud.Intelligence.Domain.Links;

public sealed record IntelligenceLink
{
    public IntelligenceLink(string sourceEntityId, string targetEntityId, IntelligenceLinkType type, int occurrences, DateTimeOffset firstSeenAtUtc, DateTimeOffset lastSeenAtUtc)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityId)) throw new ArgumentException("Source entity is required.");
        if (string.IsNullOrWhiteSpace(targetEntityId)) throw new ArgumentException("Target entity is required.");
        if (occurrences <= 0) throw new ArgumentOutOfRangeException(nameof(occurrences));
        if (lastSeenAtUtc < firstSeenAtUtc) throw new ArgumentException("Last seen time cannot precede first seen time.");
        SourceEntityId = sourceEntityId.Trim();
        TargetEntityId = targetEntityId.Trim();
        Type = type;
        Occurrences = occurrences;
        FirstSeenAtUtc = firstSeenAtUtc;
        LastSeenAtUtc = lastSeenAtUtc;
    }

    public string SourceEntityId { get; }
    public string TargetEntityId { get; }
    public IntelligenceLinkType Type { get; }
    public int Occurrences { get; }
    public DateTimeOffset FirstSeenAtUtc { get; }
    public DateTimeOffset LastSeenAtUtc { get; }
}