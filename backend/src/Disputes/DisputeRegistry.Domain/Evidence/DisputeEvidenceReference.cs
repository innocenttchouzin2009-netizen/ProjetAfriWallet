namespace AfriWallet.Disputes.Registry.Domain.Evidence;

public sealed record DisputeEvidenceReference
{
    public DisputeEvidenceReference(Guid evidenceId, DisputeEvidenceType type, string referenceId, string summary, DateTimeOffset linkedAtUtc)
    {
        if (evidenceId == Guid.Empty)
            throw new ArgumentException("Evidence id is required.", nameof(evidenceId));
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("Evidence reference id is required.", nameof(referenceId));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Evidence summary is required.", nameof(summary));

        EvidenceId = evidenceId;
        Type = type;
        ReferenceId = referenceId.Trim();
        Summary = summary.Trim();
        LinkedAtUtc = linkedAtUtc;
    }

    public Guid EvidenceId { get; }
    public DisputeEvidenceType Type { get; }
    public string ReferenceId { get; }
    public string Summary { get; }
    public DateTimeOffset LinkedAtUtc { get; }
}
