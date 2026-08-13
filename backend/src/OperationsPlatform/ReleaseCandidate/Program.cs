using Operations.ReleaseCandidate.Manifest;
using Operations.ReleaseCandidate.Reports;
using Operations.ReleaseCandidate.Validation;

var repositoryRoot =
    FindRepositoryRoot();

var releaseDirectory =
    Path.Combine(
        repositoryRoot,
        "release",
        "operations-platform",
        "v1.2.0-rc1");

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
    "rollback"
})
{
    Directory.CreateDirectory(
        Path.Combine(
            releaseDirectory,
            directory));
}

var validator =
    new OperationsRcValidator();

var summary =
    validator.Execute();

var reportWriter =
    new ReleaseReportWriter();

await reportWriter.WriteAsync(
    summary,
    releaseDirectory);

var manifestWriter =
    new ReleaseManifestWriter();

await manifestWriter.WriteAsync(
    releaseDirectory);

var checksumWriter =
    new ChecksumWriter();

await checksumWriter.WriteAsync(
    releaseDirectory);

foreach (var check in summary.Checks)
{
    Console.WriteLine(
        $"{check.Name,-35} {(check.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Total}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();

Console.WriteLine(
    summary.Success
        ? "Decision: READY FOR OPERATIONS RC"
        : "Decision: NOT READY");

if (!summary.Success)
{
    Environment.ExitCode = 1;
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

        current = current.Parent;
    }

    throw new InvalidOperationException(
        "Repository root could not be located.");
}
