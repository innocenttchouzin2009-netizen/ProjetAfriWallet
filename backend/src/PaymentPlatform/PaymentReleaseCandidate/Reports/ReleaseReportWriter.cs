using System.Text.Json;
using AfriWallet.PaymentPlatform.ReleaseCandidate.Validation;

namespace AfriWallet.PaymentPlatform.ReleaseCandidate.Reports;

public sealed class ReleaseReportWriter
{
    public async Task WriteAsync(
        ReleaseValidationSummary summary,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(
            outputDirectory);

        var decision =
            summary.Success
                ? "READY FOR PAYMENT RC"
                : "NOT READY";

        var json =
            JsonSerializer.Serialize(
                new
                {
                    delivery =
                        "AFW-DLV-0014.8",

                    version =
                        "v1.4.0-rc1",

                    checks =
                        summary.Total,

                    passed =
                        summary.Passed,

                    failed =
                        summary.Failed,

                    skipped =
                        summary.Skipped,

                    decision,

                    generatedAtUtc =
                        DateTimeOffset.UtcNow,

                    results =
                        summary.Checks.Select(
                            x => new
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
            Path.Combine(
                outputDirectory,
                "validation-report.json"),
            json,
            cancellationToken);

        var lines =
            new List<string>
            {
                "# AFW-DLV-0014.8 — Payment Platform Release Candidate",
                "",
                "Version: v1.4.0-rc1",
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
        lines.Add($"Decision: {decision}");

        await File.WriteAllLinesAsync(
            Path.Combine(
                outputDirectory,
                "validation-report.md"),
            lines,
            cancellationToken);
    }
}
