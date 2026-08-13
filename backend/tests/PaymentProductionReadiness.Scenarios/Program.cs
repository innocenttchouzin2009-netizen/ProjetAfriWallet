using AfriWallet.PaymentPlatform.ProductionReadiness.Packaging;
using AfriWallet.PaymentPlatform.ProductionReadiness.Validation;

var repositoryRoot = FindRepositoryRoot();
var evidenceDirectory = Environment.GetEnvironmentVariable(
    "AFW_PAYMENT_READINESS_EVIDENCE") ?? Path.Combine(
    repositoryRoot,
    "build",
    "payment-readiness-evidence");
var releaseDirectory = Path.Combine(
    repositoryRoot,
    "release",
    "payment-platform",
    "v1.4.0");

var summary = new PaymentPlatformReadinessValidator(
    repositoryRoot,
    evidenceDirectory).Run();

foreach (var check in summary.Checks)
{
    Console.WriteLine($"{check.Name,-38} {(check.Passed ? "PASS" : "FAIL")}");
}

Check("readiness check count", summary.Total == 22);
Check("readiness pass count", summary.Passed == 22);
Check("readiness failure count", summary.Failed == 0);
Check("readiness skipped count", summary.Skipped == 0);

await new ReleasePackageVerifier().VerifyAsync(releaseDirectory);
Check("release package verification", true);

var missingEvidenceSummary = new PaymentPlatformReadinessValidator(
    repositoryRoot,
    Path.Combine(Path.GetTempPath(), $"afw-missing-evidence-{Guid.NewGuid():N}"))
    .Run();

Check(
    "missing evidence fails closed",
    !missingEvidenceSummary.Success && missingEvidenceSummary.Failed > 0);

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Total}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();
Console.WriteLine("Decision: READY FOR PAYMENT RC");
Console.WriteLine();
Console.WriteLine(
    "All AFW-DLV-0014.7 payment production-readiness scenarios passed.");

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-38} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-38} PASS");
}

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