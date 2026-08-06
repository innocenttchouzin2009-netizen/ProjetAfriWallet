using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using RegulatoryReporting.Contracts;
using RegulatoryReporting.Domain;

namespace RegulatoryReporting.Application;

public sealed class RegulatoryReportGenerator
{
    public (RegulatoryReport report, double durationMs) Generate(RegulatoryReport report)
    {
        var stopwatch = Stopwatch.StartNew();

        report.AggregationSummary =
            $"Fraud alerts={report.EvidenceReferences.Count(x => x.SourceType.Contains("FRAUD", StringComparison.OrdinalIgnoreCase))}; " +
            $"AML alerts={report.EvidenceReferences.Count(x => x.SourceType.Contains("AML", StringComparison.OrdinalIgnoreCase))}; " +
            $"Risk insights={report.EvidenceReferences.Count(x => x.SourceType.Contains("RISK", StringComparison.OrdinalIgnoreCase))}; " +
            $"Device signals={report.EvidenceReferences.Count(x => x.SourceType.Contains("DEVICE", StringComparison.OrdinalIgnoreCase))}; " +
            $"Compliance decisions={report.Decisions.Count}.";

        report.GeneratedAtUtc = DateTimeOffset.UtcNow;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var checksum = ComputeChecksum(report);
        report.Checksum = checksum;

        stopwatch.Stop();
        return (report, stopwatch.Elapsed.TotalMilliseconds);
    }

    public ReportChecksum ComputeChecksum(RegulatoryReport report)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var raw = string.Join("|", new[]
        {
            report.ReportReference,
            report.ReportType.ToString(),
            report.JurisdictionCode,
            report.AuthorityCode,
            report.PeriodStartUtc.ToString("O"),
            report.PeriodEndUtc.ToString("O"),
            report.Status.ToString(),
            report.CurrentVersion.ToString(),
            string.Join(",", report.SourceCaseIds),
            string.Join(",", report.SubjectAwidIds),
            string.Join(",", report.Decisions),
            string.Join(",", report.EvidenceReferences.Select(x => x.SourceId))
        });

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new ReportChecksum
        {
            Algorithm = "SHA-256",
            Value = hash,
            GeneratedAtUtc = generatedAt,
            ReportReference = report.ReportReference,
            ReportVersion = report.CurrentVersion
        };
    }

    public RegulatoryReport BuildFromCreateRequest(CreateRegulatoryReportRequest request)
    {
        var type = Enum.Parse<RegulatoryReportType>(request.ReportType, ignoreCase: true);

        var report = new RegulatoryReport
        {
            ReportReference = $"RPT-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32],
            ReportType = type,
            JurisdictionCode = request.JurisdictionCode,
            AuthorityCode = request.AuthorityCode,
            SourceCaseIds = request.SourceCaseIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SubjectAwidIds = request.SubjectAwidIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            PeriodStartUtc = request.PeriodStartUtc,
            PeriodEndUtc = request.PeriodEndUtc,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId
        };

        report.EvidenceReferences.AddRange(CreateEvidenceReferences(report));
        report.Decisions.Add("Pending compliance review");
        report.InvestigationNotes.Add("Snapshot references created from integrated risk engines.");
        return report;
    }

    private static IEnumerable<ReportEvidenceReference> CreateEvidenceReferences(RegulatoryReport report)
    {
        foreach (var caseId in report.SourceCaseIds)
        {
            yield return new ReportEvidenceReference
            {
                SourceSystem = "RiskPlatform",
                SourceType = "COMPLIANCE_CASE",
                SourceId = caseId,
                Summary = "Compliance case reference"
            };

            yield return new ReportEvidenceReference
            {
                SourceSystem = "FraudDetection",
                SourceType = "FRAUD_ALERT",
                SourceId = $"fraud-{caseId}",
                Summary = "Fraud signal summary reference"
            };

            yield return new ReportEvidenceReference
            {
                SourceSystem = "AMLMonitoring",
                SourceType = "AML_ALERT",
                SourceId = $"aml-{caseId}",
                Summary = "AML signal summary reference"
            };

            yield return new ReportEvidenceReference
            {
                SourceSystem = "RiskScoring",
                SourceType = "RISK_EXPLANATION",
                SourceId = $"risk-{caseId}",
                Summary = "Risk score explanation reference"
            };

            yield return new ReportEvidenceReference
            {
                SourceSystem = "DeviceIntelligence",
                SourceType = "DEVICE_SIGNAL",
                SourceId = $"device-{caseId}",
                Summary = "Device intelligence signal reference"
            };
        }
    }
}
