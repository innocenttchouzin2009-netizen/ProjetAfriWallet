using AfriWallet.PaymentPlatform.ReleaseCandidate.Packaging;
using AfriWallet.PaymentPlatform.ReleaseCandidate.Reports;
using AfriWallet.PaymentPlatform.ReleaseCandidate.Validation;

var repositoryRoot =
    FindRepositoryRoot();

var releaseDirectory =
    Path.Combine(
        repositoryRoot,
        "release",
        "payment-platform",
        "v1.4.0-rc1");

Directory.CreateDirectory(
    releaseDirectory);

foreach (var directory in new[]
{
    "openapi",
    "adr",
    "runbooks",
    "dashboards",
    "configuration",
    "artifacts",
    "evidence",
    "rollback"
})
{
    Directory.CreateDirectory(
        Path.Combine(
            releaseDirectory,
            directory));
}

EnsureSeedFile(
    releaseDirectory,
    "release-notes.md",
    "# AfriWallet Payment Platform v1.4.0-rc1");

EnsureSeedFile(
    releaseDirectory,
    "changelog.md",
    "# Payment Platform Changelog");

var validator =
    new PaymentRcValidator();

var summary =
    validator.Run();

await new ReleaseReportWriter()
    .WriteAsync(
        summary,
        releaseDirectory);

await new ReleaseManifestWriter()
    .WriteAsync(
        releaseDirectory);

await new ChecksumWriter()
    .WriteAsync(
        releaseDirectory);

new ReleasePackageValidator()
    .Validate(
        releaseDirectory);

foreach (var check in summary.Checks)
{
    Console.WriteLine(
        $"{check.Name,-42} {(check.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Total}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();

Console.WriteLine(
    summary.Success
        ? "Decision: READY FOR PAYMENT RC"
        : "Decision: NOT READY");

if (!summary.Success)
{
    Environment.ExitCode = 1;
}

static void EnsureSeedFile(
    string directory,
    string fileName,
    string heading)
{
    var path =
        Path.Combine(
            directory,
            fileName);

    if (!File.Exists(path))
    {
        File.WriteAllText(
            path,
            $"{heading}{Environment.NewLine}");
    }
}

static string FindRepositoryRoot()
{
    var current =
        new DirectoryInfo(
            Directory.GetCurrentDirectory());

    while (current is not null)
    {
        if (Directory.Exists(
                Path.Combine(
                    current.FullName,
                    ".git")) ||
            File.Exists(
                Path.Combine(
                    current.FullName,
                    ".git")))
        {
            return current.FullName;
        }

        current =
            current.Parent;
    }

    throw new InvalidOperationException(
        "Repository root could not be located.");
}
