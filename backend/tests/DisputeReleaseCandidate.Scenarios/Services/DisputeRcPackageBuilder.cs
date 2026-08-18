using System.Security.Cryptography;
using System.Text.Json;
using AfriWallet.Disputes.ReleaseCandidate.Models;

namespace AfriWallet.Disputes.ReleaseCandidate.Services;

public sealed class DisputeRcPackageBuilder
{
    public async Task BuildAsync(string repositoryRoot, RcReport report, CancellationToken cancellationToken = default)
    {
        var releaseRoot = Path.Combine(repositoryRoot, "release", "dispute-platform", "v1.8.0-rc1");
        Directory.CreateDirectory(releaseRoot);
        foreach (var directory in new[] { "runbooks", "evidence", "configuration", "rollback", "artifacts" })
            Directory.CreateDirectory(Path.Combine(releaseRoot, directory));

        await WriteValidationReportJsonAsync(releaseRoot, report, cancellationToken);
        await WriteTagsAsync(releaseRoot, report, cancellationToken);
        await WriteManifestAsync(releaseRoot, cancellationToken);
    }

    private static async Task WriteValidationReportJsonAsync(string releaseRoot, RcReport report, CancellationToken cancellationToken)
    {
        var payload = new
        {
            total = report.Total,
            passed = report.Passed,
            failed = report.Failed,
            skipped = report.Skipped,
            decision = report.Ready ? "READY FOR DISPUTE RC" : "NOT READY"
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(releaseRoot, "validation-report.json"), json, cancellationToken);
    }

    private static async Task WriteTagsAsync(string releaseRoot, RcReport report, CancellationToken cancellationToken)
    {
        var lines = report.Checks
            .Where(x => x.Code.StartsWith("sprint18-dlv-", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{x.Code}  {x.Evidence}")
            .ToArray();

        await File.WriteAllLinesAsync(Path.Combine(releaseRoot, "delivery-tags.txt"), lines, cancellationToken);
    }

    private static async Task WriteManifestAsync(string releaseRoot, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(releaseRoot, "manifest.sha256");
        var files = Directory
            .EnumerateFiles(releaseRoot, "*", SearchOption.AllDirectories)
            .Where(x => !string.Equals(x, manifestPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

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
