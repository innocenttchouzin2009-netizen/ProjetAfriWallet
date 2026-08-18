using System.Diagnostics;
using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class FrozenTagCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-002";

    private static readonly string[] Tags =
    {
        "sprint18-dlv-0018.1",
        "sprint18-dlv-0018.2",
        "sprint18-dlv-0018.3",
        "sprint18-dlv-0018.4",
        "sprint18-dlv-0018.5",
        "sprint18-dlv-0018.6"
    };

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var tag in Tags)
        {
            var localSha = RunGit(repositoryRoot, "rev-list", "-n", "1", $"{tag}^{{}}");
            if (string.IsNullOrWhiteSpace(localSha))
                return Task.FromResult(Fail($"{tag} local SHA unresolved."));

            var remote = RunGit(repositoryRoot, "ls-remote", "--tags", "origin", $"refs/tags/{tag}^{{}}");
            if (string.IsNullOrWhiteSpace(remote))
            {
                remote = RunGit(repositoryRoot, "ls-remote", "--tags", "origin", $"refs/tags/{tag}");
                if (string.IsNullOrWhiteSpace(remote))
                    return Task.FromResult(Fail($"{tag} remote tag missing."));
            }

            var remoteSha = remote.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (!string.Equals(localSha, remoteSha, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(Fail($"{tag} SHA parity failed."));

            var ancestorExitCode = RunGitExitCode(repositoryRoot, "merge-base", "--is-ancestor", localSha, "origin/main");
            if (ancestorExitCode != 0)
                return Task.FromResult(Fail($"{tag} is not part of origin/main."));
        }

        return Task.FromResult(
            new ReadinessCheck(Code, "Frozen dispute delivery tags", ReadinessStatus.Passed, "0018.1 through 0018.6 tag parity verified"));
    }

    private ReadinessCheck Fail(string evidence) => new(Code, "Frozen dispute delivery tags", ReadinessStatus.Failed, evidence);

    private static string RunGit(string root, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start git.");
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? stdout.Trim() : string.Empty;
    }

    private static int RunGitExitCode(string root, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start git.");
        process.WaitForExit();
        return process.ExitCode;
    }
}
