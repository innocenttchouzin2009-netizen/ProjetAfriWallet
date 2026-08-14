using AfriWallet.BankingPlatform.BankingReleaseCandidate.Packaging;
using AfriWallet.BankingPlatform.BankingReleaseCandidate.Reports;
using AfriWallet.BankingPlatform.BankingReleaseCandidate.Validation;

var validator = new BankingRcValidator();
var summary = validator.Run();

var root = FindRepositoryRoot();
var releaseDirectory = Path.Combine(root, "release", "banking-platform", "v1.5.0-rc1");

Directory.CreateDirectory(releaseDirectory);

EnsureFile(releaseDirectory, "release-notes.md", "# AfriWallet Banking Platform v1.5.0-rc1");
EnsureFile(releaseDirectory, "changelog.md", "# Banking Platform Changelog");
EnsureFile(releaseDirectory, "validation-report.json", "{}");
EnsureFile(releaseDirectory, "validation-report.md", "# Validation");

foreach (var directory in new[]
{
    "openapi",
    "adr",
    "runbooks",
    "dashboards",
    "configuration",
    "evidence",
    "artifacts",
    "rollback"
})
{
    Directory.CreateDirectory(Path.Combine(releaseDirectory, directory));
}

await new RcReportWriter().WriteAsync(summary, releaseDirectory);
await new RcManifestWriter().WriteAsync(releaseDirectory);
await new RcChecksumWriter().WriteAsync(releaseDirectory);
new RcPackageValidator().Validate(releaseDirectory);

foreach (var check in summary.Checks)
{
    Console.WriteLine($"{check.Name,-46} {(check.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Total}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();
Console.WriteLine(summary.Success ? "Decision: READY FOR BANKING RC" : "Decision: NOT READY");

if (!summary.Success)
{
    Environment.ExitCode = 1;
}

static void EnsureFile(string directory, string fileName, string heading)
{
    var path = Path.Combine(directory, fileName);
    if (!File.Exists(path))
    {
        File.WriteAllText(path, heading + Environment.NewLine);
    }
}

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Repository root not found.");
}
