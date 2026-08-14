using System.Text.Json;
using AfriWallet.BankingPlatform.BankingReleaseCandidate.Validation;

namespace AfriWallet.BankingPlatform.BankingReleaseCandidate.Reports;

public sealed class RcReportWriter
{
    public async Task WriteAsync(
        RcValidationSummary summary,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var decision = summary.Success ? "READY FOR BANKING RC" : "NOT READY";

        var report = new
        {
            delivery = "AFW-DLV-0015.8",
            version = "v1.5.0-rc1",
            checks = summary.Total,
            passed = summary.Passed,
            failed = summary.Failed,
            skipped = summary.Skipped,
            decision,
            generatedAtUtc = DateTimeOffset.UtcNow,
            results = summary.Checks.Select(x => new
            {
                name = x.Name,
                passed = x.Passed,
                details = x.Details
            })
        };

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "validation-report.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);

        var lines = new List<string>
        {
            "# AFW-DLV-0015.8 — Banking Platform Release Candidate",
            string.Empty,
            "Version: v1.5.0-rc1",
            string.Empty
        };

        foreach (var check in summary.Checks)
        {
            lines.Add($"- {check.Name}: {(check.Passed ? "PASS" : "FAIL")}");
        }

        lines.Add(string.Empty);
        lines.Add($"Checks: {summary.Total}");
        lines.Add($"Passed: {summary.Passed}");
        lines.Add($"Failed: {summary.Failed}");
        lines.Add($"Skipped: {summary.Skipped}");
        lines.Add(string.Empty);
        lines.Add($"Decision: {decision}");

        await File.WriteAllLinesAsync(
            Path.Combine(outputDirectory, "validation-report.md"),
            lines,
            cancellationToken);
    }
}
