using System.Security.Cryptography;
using System.Text.Json;

namespace Operations.ReleaseCandidate.Manifest;

public sealed class ReleaseManifestWriter
{
    public async Task WriteAsync(
        string releaseDirectory,
        CancellationToken cancellationToken = default)
    {
        var files = Directory
            .EnumerateFiles(
                releaseDirectory,
                "*",
                SearchOption.AllDirectories)
            .Where(path =>
                !path.EndsWith(
                    "manifest.json",
                    StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(
                    "checksums.sha256",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        var entries = new List<object>();

        foreach (var file in files)
        {
            var hash =
                await ComputeSha256Async(
                    file,
                    cancellationToken);

            entries.Add(new
            {
                path = Path.GetRelativePath(
                    releaseDirectory,
                    file)
                    .Replace('\\', '/'),
                sha256 = hash,
                size = new FileInfo(file).Length
            });
        }

        var manifest = new
        {
            delivery = "AFW-DLV-0012.8",
            version = "v1.2.0-rc1",
            generatedAtUtc = DateTime.UtcNow,
            files = entries
        };

        var json =
            JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            Path.Combine(
                releaseDirectory,
                "manifest.json"),
            json,
            cancellationToken);
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream =
            File.OpenRead(path);

        var hash =
            await SHA256.HashDataAsync(
                stream,
                cancellationToken);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}
