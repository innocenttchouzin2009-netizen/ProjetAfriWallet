using System.Security.Cryptography;

namespace TreasuryReleaseCandidate.Manifest;

public sealed class ChecksumWriter
{
    public void Write(string releaseRoot, string outputFileName)
    {
        var outputPath = Path.Combine(releaseRoot, outputFileName);

        var files = Directory
            .GetFiles(releaseRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var lines = new List<string>();

        foreach (var file in files)
        {
            var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
            var relative = Path.GetRelativePath(releaseRoot, file).Replace('\\', '/');
            lines.Add($"{hash}  {relative}");
        }

        File.WriteAllLines(outputPath, lines);
    }
}
