using System.Diagnostics;
using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class FrozenTagCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-002";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var tags = Enumerable.Range(1, 6).Select(x => $"sprint17-dlv-0017.{x}").ToArray();
        var failures = new List<string>();
        foreach (var tag in tags)
        {
            var local = RunGit(root, "rev-list", "-n", "1", $"{tag}^{{}}");
            if (string.IsNullOrWhiteSpace(local)) { failures.Add($"{tag} local SHA unresolved"); continue; }
            var remote = RunGit(root, "ls-remote", "--tags", "origin", $"refs/tags/{tag}^{{}}");
            if (string.IsNullOrWhiteSpace(remote)) { failures.Add($"{tag} remote tag missing"); continue; }
            var remoteSha = remote.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
            if (!string.Equals(local, remoteSha, StringComparison.OrdinalIgnoreCase)) failures.Add($"{tag} parity failure");
            if (RunGitExitCode(root, "merge-base", "--is-ancestor", local, "origin/main") != 0) failures.Add($"{tag} not in origin/main");
        }
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Frozen delivery tags", failures.Count == 0, failures.Count == 0 ? "0017.1 through 0017.6 tag parity verified" : string.Join(", ", failures)));
    }

    private static string RunGit(string root, params string[] args)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start git.");
        var stdout = process.StandardOutput.ReadToEnd(); process.WaitForExit();
        return process.ExitCode == 0 ? stdout.Trim() : string.Empty;
    }

    private static int RunGitExitCode(string root, params string[] args)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start git."); process.WaitForExit(); return process.ExitCode;
    }
}