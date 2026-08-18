namespace AfriWallet.Disputes.Readiness.Checks;

internal static class RepositoryCheckUtilities
{
    public static string Resolve(string root, params string[] parts)
    {
        var path = root;
        foreach (var part in parts)
            path = Path.Combine(path, part);
        return Path.GetFullPath(path);
    }

    public static IEnumerable<string> EnumerateTextFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return [];

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".csproj", ".json", ".md", ".ps1", ".yml", ".yaml"
        };

        return Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(x => allowed.Contains(Path.GetExtension(x)))
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }
}
