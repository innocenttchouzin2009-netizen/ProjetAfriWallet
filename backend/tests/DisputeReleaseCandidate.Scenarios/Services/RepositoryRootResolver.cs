namespace AfriWallet.Disputes.ReleaseCandidate.Services;

public static class RepositoryRootResolver
{
    public static string Resolve()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "backend")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException("AfriWallet repository root not found.");
    }
}
