using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

internal static class RepositoryCheckUtilities
{
    public static string Resolve(string repositoryRoot, params string[] parts) => Path.GetFullPath(Path.Combine(new[] { repositoryRoot }.Concat(parts).ToArray()));

    public static IEnumerable<string> EnumerateTextFiles(string directory)
    {
        if (!Directory.Exists(directory)) return [];
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".cs", ".csproj", ".json", ".md", ".ps1", ".yml", ".yaml" };
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(x => allowed.Contains(Path.GetExtension(x)))
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    public static ReadinessCheck Result(string code, string name, bool passed, string evidence) => new(code, name, passed ? ReadinessStatus.Passed : ReadinessStatus.Failed, evidence);
}