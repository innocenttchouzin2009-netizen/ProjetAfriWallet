namespace AfriWallet.PaymentPlatform.ReleaseCandidate.Packaging;

public sealed class ReleasePackageValidator
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
        "artifacts",
        "evidence",
        "rollback"
    ];

    public void Validate(
        string releaseDirectory)
    {
        foreach (var file in RequiredFiles)
        {
            var path =
                Path.Combine(
                    releaseDirectory,
                    file);

            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Required RC file missing: {file}");
            }
        }

        foreach (var directory in RequiredDirectories)
        {
            var path =
                Path.Combine(
                    releaseDirectory,
                    directory);

            if (!Directory.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Required RC directory missing: {directory}");
            }
        }
    }
}
