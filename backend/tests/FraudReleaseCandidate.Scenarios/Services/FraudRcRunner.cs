using AfriWallet.Fraud.ReleaseCandidate.Models;

namespace AfriWallet.Fraud.ReleaseCandidate.Services;

public sealed class FraudRcRunner(string repositoryRoot, GitDeliveryVerifier git)
{
    private static readonly string[] Tags = Enumerable.Range(1, 7).Select(x => $"sprint17-dlv-0017.{x}").ToArray();
    private static readonly string[] PackageFiles = { "release-notes.md", "changelog.md", "validation-report.json", "validation-report.md", "manifest.sha256", "delivery-tags.txt" };
    private static readonly string[] PackageDirectories = { "runbooks", "evidence", "configuration", "rollback", "artifacts" };
    private string ReleaseRoot => Path.Combine(repositoryRoot, "release", "fraud-platform", "v1.7.0-rc1");

    public RcReport Run()
    {
        var checks = new List<RcCheck>();
        checks.AddRange(Tags.Select(VerifyTag));
        checks.AddRange(PackageFiles.Select(VerifyFile));
        checks.AddRange(PackageDirectories.Select(VerifyDirectory));
        return new RcReport(checks);
    }

    private RcCheck VerifyTag(string tag)
    {
        try
        {
            var local = git.ResolveTagCommit(tag);
            var remote = git.ResolveRemotePeeledTag(tag);
            if (!string.Equals(local, remote, StringComparison.OrdinalIgnoreCase)) return Fail(tag, "Frozen fraud delivery tag", $"Local={local}; Remote={remote}");
            if (!git.IsCommitInMain(local)) return Fail(tag, "Frozen fraud delivery tag", $"{local} not in origin/main");
            return Pass(tag, "Frozen fraud delivery tag", $"{local} VERIFIED");
        }
        catch (Exception ex) { return Fail(tag, "Frozen fraud delivery tag", ex.Message); }
    }

    private RcCheck VerifyFile(string file)
    {
        var path = Path.Combine(ReleaseRoot, file);
        return File.Exists(path) ? Pass($"RC-FILE-{file}", "RC package file", $"{file} present") : Fail($"RC-FILE-{file}", "RC package file", $"Missing {file}");
    }

    private RcCheck VerifyDirectory(string directory)
    {
        var path = Path.Combine(ReleaseRoot, directory);
        return Directory.Exists(path) ? Pass($"RC-DIR-{directory}", "RC package directory", $"{directory} present") : Fail($"RC-DIR-{directory}", "RC package directory", $"Missing {directory}");
    }

    private static RcCheck Pass(string code, string name, string evidence) => new(code, name, true, evidence);
    private static RcCheck Fail(string code, string name, string evidence) => new(code, name, false, evidence);
}