namespace AfriWallet.BankingPlatform.BankingReleaseCandidate.Packaging;

public sealed class RcPackageValidator
{
    private static readonly string[] RequiredFiles =
    [
        "validation-report.json",
        "validation-report.md",
        "release-notes.md",
        "changelog.md",
        "manifest.json",
        "checksums.sha256"
    ];

    private static readonly string[] RequiredDirectories =
    [
        "openapi",
        "adr",
        "runbooks",
        "dashboards",
        "configuration",
        "evidence",
        "artifacts",
        "rollback"
    ];

    public void Validate(string releaseDirectory)
    {
        foreach (var file in RequiredFiles)
        {
            if (!File.Exists(Path.Combine(releaseDirectory, file)))
            {
                throw new InvalidOperationException($"Required RC file missing: {file}");
            }
        }

        foreach (var directory in RequiredDirectories)
        {
            if (!Directory.Exists(Path.Combine(releaseDirectory, directory)))
            {
                throw new InvalidOperationException($"Required RC directory missing: {directory}");
            }
        }
    }
}
