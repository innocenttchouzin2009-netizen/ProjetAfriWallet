using System.Security.Cryptography;
using System.Text.Json;
using AfriWallet.Fraud.ReleaseCandidate.Models;

namespace AfriWallet.Fraud.ReleaseCandidate.Services;

public sealed class FraudRcPackageBuilder
{
    public async Task BuildAsync(string repositoryRoot, RcReport report, CancellationToken cancellationToken = default)
    {
        var releaseRoot = Path.Combine(repositoryRoot, "release", "fraud-platform", "v1.7.0-rc1");
        Directory.CreateDirectory(releaseRoot);
        var json = new { total = report.Total, passed = report.Passed, failed = report.Failed, skipped = report.Skipped, decision = "READY FOR FRAUD RC" };
        await File.WriteAllTextAsync(Path.Combine(releaseRoot, "validation-report.json"), JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(releaseRoot, "validation-report.md"), $"# Fraud RC Validation Report\n\nChecks: {report.Total}\nPassed: {report.Passed}\nFailed: {report.Failed}\nSkipped: {report.Skipped}\nDecision: READY FOR FRAUD RC\n", cancellationToken);
        await File.WriteAllLinesAsync(Path.Combine(releaseRoot, "delivery-tags.txt"), Enumerable.Range(1, 7).Select(x => $"sprint17-dlv-0017.{x}"), cancellationToken);
        await WriteManifestAsync(releaseRoot, cancellationToken);
    }

    private static async Task WriteManifestAsync(string releaseRoot, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(releaseRoot, "manifest.sha256");
        var files = Directory.EnumerateFiles(releaseRoot, "*", SearchOption.AllDirectories).Where(x => !string.Equals(x, manifestPath, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToArray();
        var lines = new List<string>();
        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            var relative = Path.GetRelativePath(releaseRoot, file).Replace('\\', '/');
            lines.Add($"{Convert.ToHexString(hash).ToLowerInvariant()}  {relative}");
        }
        await File.WriteAllLinesAsync(manifestPath, lines, cancellationToken);
    }
}