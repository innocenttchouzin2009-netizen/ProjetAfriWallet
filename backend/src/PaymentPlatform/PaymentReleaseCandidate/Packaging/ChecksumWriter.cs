using System.Security.Cryptography;

namespace AfriWallet.PaymentPlatform.ReleaseCandidate.Packaging;

public sealed class ChecksumWriter
{
    public async Task WriteAsync(
        string releaseDirectory,
        CancellationToken cancellationToken = default)
    {
        var checksumPath =
            Path.Combine(
                releaseDirectory,
                "checksums.sha256");

        var files =
            Directory
                .EnumerateFiles(
                    releaseDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .Where(x =>
                    !string.Equals(
                        x,
                        checksumPath,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x)
                .ToArray();

        var lines =
            new List<string>();

        foreach (var file in files)
        {
            await using var stream =
                File.OpenRead(file);

            var hash =
                await SHA256.HashDataAsync(
                    stream,
                    cancellationToken);

            var relative =
                Path.GetRelativePath(
                        releaseDirectory,
                        file)
                    .Replace('\\', '/');

            lines.Add(
                $"{Convert.ToHexString(hash).ToLowerInvariant()}  {relative}");
        }

        await File.WriteAllLinesAsync(
            checksumPath,
            lines,
            cancellationToken);
    }
}
