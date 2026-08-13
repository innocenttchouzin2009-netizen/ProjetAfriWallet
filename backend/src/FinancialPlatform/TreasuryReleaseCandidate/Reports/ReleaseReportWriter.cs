using System.Text.Json;
using TreasuryReleaseCandidate.Validation;

namespace TreasuryReleaseCandidate.Reports;

public sealed class ReleaseReportWriter
{
    public void Write(
        string releaseRoot,
        string version,
        string stream,
        string decision,
        ReleaseValidationSummary summary)
    {
        var jsonPath = Path.Combine(releaseRoot, "validation-report.json");
        var markdownPath = Path.Combine(releaseRoot, "validation-report.md");

        var report = new
        {
            version,
            stream,
            decision,
            checks = summary.Checks.ToDictionary(check => check.Name, check => check.Passed),
            summary = new
            {
                checks = summary.Checks.Count,
                passed = summary.Passed,
                failed = summary.Failed,
                skipped = summary.Skipped
            }
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(jsonPath, json);

        var lines = new List<string>
        {
            "# Treasury Release Candidate Validation",
            string.Empty,
            $"- Version: {version}",
            $"- Stream: {stream}",
            $"- Decision: {decision}",
            $"- Checks: {summary.Checks.Count}",
            $"- Passed: {summary.Passed}",
            $"- Failed: {summary.Failed}",
            $"- Skipped: {summary.Skipped}",
            string.Empty,
            "## Checks"
        };

        lines.AddRange(summary.Checks.Select(check => $"- {(check.Passed ? "PASS" : "FAIL")}: {check.Name} ({check.Details})"));
        File.WriteAllLines(markdownPath, lines);
    }
}
