namespace AfriWallet.Fraud.ReleaseCandidate.Services;

public static class RepositoryRootResolver
{
    public static string Resolve(string? explicitRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot) && Directory.Exists(Path.Combine(explicitRoot, "backend"))) return Path.GetFullPath(explicitRoot);
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) && Directory.Exists(Path.Combine(current.FullName, "backend"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("AfriWallet repository root not found.");
    }
}