namespace RegulatoryReporting.Domain;

public sealed class ReportChecksum
{
    public string Algorithm { get; set; } = "SHA-256";
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int ReportVersion { get; set; }
    public string ReportReference { get; set; } = string.Empty;
    public string? Signature { get; set; }
}
