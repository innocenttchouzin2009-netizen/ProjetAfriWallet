using System.Security.Cryptography;
using System.Text.Json;

namespace AfriWallet.BankingPlatform.BankingReleaseCandidate.Packaging;

public sealed class RcManifestWriter
{
    public async Task WriteAsync(string releaseDirectory, CancellationToken cancellationToken = default)
    {
        var files = Directory
            .EnumerateFiles(releaseDirectory, "*", SearchOption.AllDirectories)
            .Where(x =>
                !x.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) &&
                !x.EndsWith("checksums.sha256", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        var entries = new List<object>();
        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            entries.Add(new
            {
                path = Path.GetRelativePath(releaseDirectory, file).Replace('\\', '/'),
                sha256 = Convert.ToHexString(hash).ToLowerInvariant(),
                sizeBytes = new FileInfo(file).Length
            });
        }

        var manifest = new
        {
            platform = "AfriWallet Banking Platform",
            delivery = "AFW-DLV-0015.8",
            version = "v1.5.0-rc1",
            generatedAtUtc = DateTimeOffset.UtcNow,
            files = entries
        };

        await File.WriteAllTextAsync(
            Path.Combine(releaseDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
    }
}
