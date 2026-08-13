using System.Text.Json;
using AfriWallet.PaymentPlatform.ProductionReadiness.Packaging;
using AfriWallet.PaymentPlatform.ProductionReadiness.Validation;

var repositoryRoot = FindRepositoryRoot();
var releaseDirectory = Path.Combine(
    repositoryRoot,
    "release",
    "payment-platform",
    "v1.4.0");
var evidenceDirectory = Environment.GetEnvironmentVariable(
    "AFW_PAYMENT_READINESS_EVIDENCE") ?? Path.Combine(
    repositoryRoot,
    "build",
    "payment-readiness-evidence");

var validator = new PaymentPlatformReadinessValidator(
    repositoryRoot,
    evidenceDirectory);
var summary = validator.Run();

foreach (var check in summary.Checks)
{
    Console.WriteLine($"{check.Name,-38} {(check.Passed ? "PASS" : "FAIL")}");

    if (!check.Passed)
        Console.WriteLine($"  {check.Details}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Total}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();

var decision = summary.Success ? "READY FOR PAYMENT RC" : "NOT READY";
Console.WriteLine($"Decision: {decision}");

var metadata = ReleaseMetadata.Load(releaseDirectory);
var report = new
{
    delivery = metadata.Delivery,
    release = metadata.Release,
    generatedAtUtc = metadata.GeneratedAtUtc,
    checks = summary.Total,
    passed = summary.Passed,
    failed = summary.Failed,
    skipped = summary.Skipped,
    decision,
    results = summary.Checks
};

await File.WriteAllTextAsync(
    Path.Combine(releaseDirectory, "validation-report.json"),
    JsonSerializer.Serialize(
        report,
        new JsonSerializerOptions
        {
            WriteIndented = true
        }));

var markdownLines = new List<string>
{
    "# AFW-DLV-0014.7 - Payment Platform Production Readiness",
    "",
    $"Checks: {summary.Total}",
    $"Passed: {summary.Passed}",
    $"Failed: {summary.Failed}",
    $"Skipped: {summary.Skipped}",
    "",
    $"Decision: {decision}",
    "",
    "## Evidence"
};

markdownLines.AddRange(summary.Checks.Select(check =>
    $"- [{(check.Passed ? "x" : " ")}] {check.Name}: {check.Details}"));

await File.WriteAllLinesAsync(
    Path.Combine(releaseDirectory, "validation-report.md"),
    markdownLines);

await new ReleaseManifestWriter().WriteAsync(releaseDirectory);
await new ChecksumWriter().WriteAsync(releaseDirectory);
await new ReleasePackageVerifier().VerifyAsync(releaseDirectory);

if (!summary.Success)
    Environment.ExitCode = 1;

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (current is not null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
            File.Exists(Path.Combine(current.FullName, ".git")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Repository root not found.");
}