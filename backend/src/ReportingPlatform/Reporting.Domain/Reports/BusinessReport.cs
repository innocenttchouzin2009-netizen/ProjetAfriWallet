using Reporting.Domain.Enums;

namespace Reporting.Domain.Reports;

public sealed class BusinessReport
{
    public Guid ReportId { get; init; } = Guid.NewGuid();

    public required string ReportCode { get; init; }

    public required string Title { get; init; }

    public required string ReportType { get; init; }

    public required DateTime PeriodStartUtc { get; init; }

    public required DateTime PeriodEndUtc { get; init; }

    public ReportStatus Status { get; private set; } = ReportStatus.Draft;

    public int Version { get; private set; } = 1;

    public string? GeneratedBy { get; private set; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime? GeneratedAtUtc { get; private set; }

    public void MarkGenerated(string generatedBy)
    {
        if (Status is ReportStatus.Archived)
        {
            throw new InvalidOperationException("An archived report cannot be generated again.");
        }

        GeneratedBy = generatedBy;
        GeneratedAtUtc = DateTime.UtcNow;
        Status = ReportStatus.Generated;
    }

    public void Archive()
    {
        Status = ReportStatus.Archived;
    }
}
