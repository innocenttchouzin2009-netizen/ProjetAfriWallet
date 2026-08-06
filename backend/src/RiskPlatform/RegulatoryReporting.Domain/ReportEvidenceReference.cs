namespace RegulatoryReporting.Domain;

public sealed class ReportEvidenceReference
{
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTimeOffset ReferencedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
