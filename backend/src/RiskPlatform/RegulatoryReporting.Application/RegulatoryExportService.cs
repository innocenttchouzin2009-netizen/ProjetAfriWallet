using System.Text;
using System.Text.Json;
using RegulatoryReporting.Contracts;
using RegulatoryReporting.Domain;

namespace RegulatoryReporting.Application;

public sealed class RegulatoryExportService
{
    public ReportExportResponse Export(RegulatoryReport report, string format)
    {
        var normalized = format.Trim().ToLowerInvariant();
        return normalized switch
        {
            "json" => ExportJson(report),
            "csv" => ExportCsv(report),
            "pdf" => ExportPdf(report),
            _ => throw new InvalidOperationException($"Unsupported export format '{format}'.")
        };
    }

    private static ReportExportResponse ExportJson(RegulatoryReport report)
    {
        var payloadObject = BuildPayloadObject(report);
        var payload = JsonSerializer.Serialize(payloadObject, new JsonSerializerOptions { WriteIndented = true });
        return BuildResponse(report, "json", "application/json", payload);
    }

    private static ReportExportResponse ExportCsv(RegulatoryReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("report_reference,version,jurisdiction,authority,period_start_utc,period_end_utc,status,checksum");
        sb.AppendLine($"{report.ReportReference},{report.CurrentVersion},{report.JurisdictionCode},{report.AuthorityCode},{report.PeriodStartUtc:O},{report.PeriodEndUtc:O},{report.Status},{report.Checksum?.Value}");
        sb.AppendLine();
        sb.AppendLine("section,key,value");
        sb.AppendLine($"summary,aggregation_summary,\"{report.AggregationSummary.Replace("\"", "\"\"")}\"");

        foreach (var decision in report.Decisions)
        {
            sb.AppendLine($"decision,entry,\"{decision.Replace("\"", "\"\"")}\"");
        }

        foreach (var evidence in report.EvidenceReferences)
        {
            sb.AppendLine($"source,{evidence.SourceType},\"{evidence.SourceId}\"");
        }

        return BuildResponse(report, "csv", "text/csv", sb.ToString());
    }

    private static ReportExportResponse ExportPdf(RegulatoryReport report)
    {
        // The delivery keeps PDF generic and exportable; renderer integration can replace this text payload later.
        var sb = new StringBuilder();
        sb.AppendLine("AFRIWALLET REGULATORY REPORT");
        sb.AppendLine($"Reference: {report.ReportReference}");
        sb.AppendLine($"Version: {report.CurrentVersion}");
        sb.AppendLine($"Jurisdiction: {report.JurisdictionCode}");
        sb.AppendLine($"Authority: {report.AuthorityCode}");
        sb.AppendLine($"Period: {report.PeriodStartUtc:O} -> {report.PeriodEndUtc:O}");
        sb.AppendLine($"Status: {report.Status}");
        sb.AppendLine($"Checksum: {report.Checksum?.Value}");
        sb.AppendLine($"GeneratedAtUtc: {report.Checksum?.GeneratedAtUtc:O}");
        sb.AppendLine($"Summary: {report.AggregationSummary}");
        return BuildResponse(report, "pdf", "application/pdf", sb.ToString());
    }

    private static object BuildPayloadObject(RegulatoryReport report)
    {
        return new
        {
            report.ReportReference,
            ReportVersion = report.CurrentVersion,
            report.JurisdictionCode,
            report.AuthorityCode,
            report.PeriodStartUtc,
            report.PeriodEndUtc,
            Status = report.Status.ToString().ToUpperInvariant(),
            Summary = report.AggregationSummary,
            Sources = report.EvidenceReferences.Select(x => new { x.SourceSystem, x.SourceType, x.SourceId, x.Summary }),
            Decisions = report.Decisions,
            Checksum = report.Checksum?.Value,
            GeneratedAtUtc = report.Checksum?.GeneratedAtUtc
        };
    }

    private static ReportExportResponse BuildResponse(RegulatoryReport report, string format, string contentType, string payload)
    {
        return new ReportExportResponse
        {
            Format = format,
            ContentType = contentType,
            FileName = $"{report.ReportReference}-v{report.CurrentVersion}.{format}",
            Payload = payload,
            Checksum = report.Checksum?.Value ?? string.Empty,
            GeneratedAtUtc = report.Checksum?.GeneratedAtUtc ?? DateTimeOffset.UtcNow,
            ReportVersion = report.CurrentVersion,
            ReportReference = report.ReportReference
        };
    }
}
