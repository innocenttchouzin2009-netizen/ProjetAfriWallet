using System.Text.Json;

namespace TreasuryReleaseCandidate.Manifest;

public sealed class ReleaseManifestWriter
{
    public void Write(string releaseRoot, string name, string version, string stream, string status)
    {
        var manifestPath = Path.Combine(releaseRoot, "manifest.json");

        var files = Directory
            .GetFiles(releaseRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(releaseRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var manifest = new
        {
            name,
            version,
            stream,
            status,
            createdAt = DateTimeOffset.UtcNow,
            checks = files.Length,
            artifacts = files
        };

        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }
}
