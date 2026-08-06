namespace RegulatoryReporting.Domain;

public sealed class RegulatoryReportVersion
{
    public int VersionNumber { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string ChangeReason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DiffSummary { get; set; } = string.Empty;
    public string SnapshotSummary { get; set; } = string.Empty;
    public ReportChecksum Checksum { get; set; } = new();
}
