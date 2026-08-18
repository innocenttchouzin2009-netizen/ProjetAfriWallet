namespace AfriWallet.Disputes.Investigation.Domain.Evidence;

public sealed record EvidenceIntegrity
{
    public EvidenceIntegrity(string sha256, long sizeBytes, string contentType)
    {
        if (string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("SHA-256 is required.", nameof(sha256));
        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        Sha256 = sha256.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        ContentType = contentType.Trim();
    }

    public string Sha256 { get; }
    public long SizeBytes { get; }
    public string ContentType { get; }
}
