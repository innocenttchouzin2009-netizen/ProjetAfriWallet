using System.Text.Json;
using Operations.ReleaseCandidate.Validation;

namespace Operations.ReleaseCandidate.Reports;

public sealed class ReleaseReportWriter
{
    public async Task WriteAsync(
        ReleaseValidationSummary summary,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var jsonPath =
            Path.Combine(
                outputDirectory,
                "validation-report.json");

        var markdownPath =
            Path.Combine(
                outputDirectory,
                "validation-report.md");

        var json = JsonSerializer.Serialize(
            new
            {
                delivery =
                    "AFW-DLV-0012.8",
                version =
                    "v1.2.0-rc1",
                checks =
                    summary.Total,
                passed =
                    summary.Passed,
                failed =
                    summary.Failed,
                skipped =
                    summary.Skipped,
                decision =
                    summary.Success
                        ? "READY FOR OPERATIONS RC"
                        : "NOT READY",
                results =
                    summary.Checks.Select(x => new
                    {
                        name = x.Name,
                        passed = x.Passed,
                        details = x.Details
                    })
            },
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(
            jsonPath,
            json,
            cancellationToken);

        var lines = new List<string>
        {
            "# AFW-DLV-0012.8 - Operations Platform Release Candidate",
            "",
            "## Validation",
            ""
        };

        foreach (var check in summary.Checks)
        {
            lines.Add(
                $"- {check.Name}: {(check.Passed ? "PASS" : "FAIL")}");
        }

        lines.Add("");
        lines.Add($"Checks: {summary.Total}");
        lines.Add($"Passed: {summary.Passed}");
        lines.Add($"Failed: {summary.Failed}");
        lines.Add($"Skipped: {summary.Skipped}");
        lines.Add("");
        lines.Add(
            $"Decision: {(summary.Success ? "READY FOR OPERATIONS RC" : "NOT READY")}");

        await File.WriteAllLinesAsync(
            markdownPath,
            lines,
            cancellationToken);
    }
}
