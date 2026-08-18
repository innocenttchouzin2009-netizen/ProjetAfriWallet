using AfriWallet.Disputes.ReleaseCandidate.Models;

namespace AfriWallet.Disputes.ReleaseCandidate.Services;

public sealed class DisputeRcRunner(GitTagVerifier git)
{
    private static readonly string[] Tags =
    {
        "sprint18-dlv-0018.1",
        "sprint18-dlv-0018.2",
        "sprint18-dlv-0018.3",
        "sprint18-dlv-0018.4",
        "sprint18-dlv-0018.5",
        "sprint18-dlv-0018.6",
        "sprint18-dlv-0018.7"
    };

    public RcReport Run()
    {
        var checks = new List<RcCheck>();

        foreach (var tag in Tags)
            checks.Add(VerifyTag(tag));

        checks.Add(CheckDirectoryExists("RC-DELIVERY-001", "Dispute claim registry present", "backend/src/Disputes/DisputeRegistry.Domain"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-002", "Dispute eligibility present", "backend/src/Disputes/DisputeEligibility.Domain"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-003", "Dispute investigation present", "backend/src/Disputes/DisputeInvestigation.Domain"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-004", "Dispute decision present", "backend/src/Disputes/DisputeDecision"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-005", "Resolution orchestration present", "backend/src/Disputes/ResolutionOrchestration"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-006", "Dispute intelligence present", "backend/src/Disputes/DisputeIntelligence"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-007", "Dispute readiness gate present", "backend/tests/DisputeReadiness.Scenarios"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-008", "Dispute RC gate present", "backend/tests/DisputeReleaseCandidate.Scenarios"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-009", "Dispute RC docs present", "docs/specs/dispute-release-candidate"));
        checks.Add(CheckFileExists("RC-DELIVERY-010", "Dispute RC workflow present", ".github/workflows/dispute-rc.yml"));
        checks.Add(CheckDirectoryExists("RC-DELIVERY-011", "Dispute RC release notes present", "release/dispute-platform/v1.8.0-rc1/runbooks"));

        return new RcReport(checks);
    }

    private RcCheck VerifyTag(string tag)
    {
        try
        {
            var localSha = git.ResolvePeeledLocalSha(tag);
            if (string.IsNullOrWhiteSpace(localSha))
                return Fail(tag, "Frozen dispute delivery tag", $"{tag} local SHA unresolved.");

            var remoteSha = git.ResolvePeeledRemoteSha(tag);
            if (!string.Equals(localSha, remoteSha, StringComparison.OrdinalIgnoreCase))
                return Fail(tag, "Frozen dispute delivery tag", $"{tag} SHA parity failed.");

            if (!git.IsInOriginMain(localSha))
                return Fail(tag, "Frozen dispute delivery tag", $"{tag} is not contained in origin/main.");

            return Pass(tag, "Frozen dispute delivery tag", $"SHA {localSha} verified in origin/main");
        }
        catch (Exception ex)
        {
            return Fail(tag, "Frozen dispute delivery tag", ex.Message);
        }
    }

    private RcCheck CheckDirectoryExists(string code, string name, string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(git.RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return Directory.Exists(full)
            ? Pass(code, name, $"{relativePath} present")
            : Fail(code, name, $"{relativePath} missing");
    }

    private RcCheck CheckFileExists(string code, string name, string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(git.RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return File.Exists(full)
            ? Pass(code, name, $"{relativePath} present")
            : Fail(code, name, $"{relativePath} missing");
    }

    private static RcCheck Pass(string code, string name, string evidence) => new(code, name, true, evidence);

    private static RcCheck Fail(string code, string name, string evidence) => new(code, name, false, evidence);
}
