using System.Security.Cryptography;
using System.Text.Json;

namespace AfriWallet.PaymentPlatform.ProductionReadiness.Packaging;

public sealed class ReleaseManifestWriter
{
    public async Task WriteAsync(
        string releaseDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(releaseDirectory);
        var metadata = ReleaseMetadata.Load(releaseDirectory);

        var files = Directory
            .EnumerateFiles(releaseDirectory, "*", SearchOption.AllDirectories)
            .Where(path =>
                !path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("checksums.sha256", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
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
                size = new FileInfo(file).Length
            });
        }

        await File.WriteAllTextAsync(
            Path.Combine(releaseDirectory, "manifest.json"),
            JsonSerializer.Serialize(
                new
                {
                    delivery = metadata.Delivery,
                    release = metadata.Release,
                    generatedAtUtc = metadata.GeneratedAtUtc,
                    files = entries
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }),
            cancellationToken);
    }
}