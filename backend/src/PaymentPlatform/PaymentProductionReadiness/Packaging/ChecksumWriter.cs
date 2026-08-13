using System.Security.Cryptography;

namespace AfriWallet.PaymentPlatform.ProductionReadiness.Packaging;

public sealed class ChecksumWriter
{
    public async Task WriteAsync(
        string releaseDirectory,
        CancellationToken cancellationToken = default)
    {
        var checksumFile = Path.Combine(releaseDirectory, "checksums.sha256");

        var files = Directory
            .EnumerateFiles(releaseDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(
                path,
                checksumFile,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var lines = new List<string>();

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            var relative = Path.GetRelativePath(releaseDirectory, file).Replace('\\', '/');

            lines.Add($"{Convert.ToHexString(hash).ToLowerInvariant()}  {relative}");
        }

        await File.WriteAllLinesAsync(checksumFile, lines, cancellationToken);
    }
}