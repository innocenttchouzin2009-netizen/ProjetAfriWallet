using System.Security.Cryptography;
using System.Text.Json;

namespace AfriWallet.PaymentPlatform.ProductionReadiness.Packaging;

public sealed class ReleasePackageVerifier
{
    public async Task VerifyAsync(
        string releaseDirectory,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(releaseDirectory, "manifest.json");
        var checksumPath = Path.Combine(releaseDirectory, "checksums.sha256");

        if (!File.Exists(manifestPath) || !File.Exists(checksumPath))
            throw new InvalidOperationException("Release manifest or checksums are missing.");

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
            manifestPath,
            cancellationToken));

        var manifestEntries = document.RootElement
            .GetProperty("files")
            .EnumerateArray()
            .ToArray();
        var manifestPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in manifestEntries)
        {
            var relativePath = entry.GetProperty("path").GetString()
                ?? throw new InvalidOperationException("Manifest path is missing.");

            if (!manifestPaths.Add(relativePath))
                throw new InvalidOperationException($"Duplicate manifest path: {relativePath}");

            var expectedHash = entry.GetProperty("sha256").GetString()
                ?? throw new InvalidOperationException("Manifest hash is missing.");
            var expectedSize = entry.GetProperty("size").GetInt64();
            var path = ResolveContainedPath(releaseDirectory, relativePath);

            await VerifyFileAsync(path, expectedHash, expectedSize, cancellationToken);
        }

        var expectedManifestPaths = Directory
            .EnumerateFiles(releaseDirectory, "*", SearchOption.AllDirectories)
            .Where(path =>
                !path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("checksums.sha256", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(releaseDirectory, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        if (!manifestPaths.SetEquals(expectedManifestPaths))
            throw new InvalidOperationException("Release manifest file coverage is incomplete.");

        var checksumPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in await File.ReadAllLinesAsync(checksumPath, cancellationToken))
        {
            if (line.Length < 67 || line[64..66] != "  ")
                throw new InvalidOperationException("Invalid checksum entry.");

            var expectedHash = line[..64];
            var relativePath = line[66..];

            if (!checksumPaths.Add(relativePath))
                throw new InvalidOperationException($"Duplicate checksum path: {relativePath}");

            var path = ResolveContainedPath(releaseDirectory, relativePath);

            await VerifyFileAsync(
                path,
                expectedHash,
                new FileInfo(path).Length,
                cancellationToken);
        }

        var expectedChecksumPaths = Directory
            .EnumerateFiles(releaseDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(
                path,
                checksumPath,
                StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(releaseDirectory, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        if (!checksumPaths.SetEquals(expectedChecksumPaths))
            throw new InvalidOperationException("Release checksum file coverage is incomplete.");
    }

    private static string ResolveContainedPath(
        string releaseDirectory,
        string relativePath)
    {
        var root = Path.GetFullPath(releaseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Release entry escapes the package root.");

        return path;
    }

    private static async Task VerifyFileAsync(
        string path,
        string expectedHash,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != expectedSize)
            throw new InvalidOperationException($"Release file is missing or changed: {path}");

        await using var stream = File.OpenRead(path);
        var actualHash = Convert
            .ToHexString(await SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();

        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Release checksum mismatch: {path}");
    }
}