using System.Diagnostics;

namespace AfriWallet.Fraud.ReleaseCandidate.Services;

public sealed class GitDeliveryVerifier(string repositoryRoot)
{
    public string ResolveTagCommit(string tag) => ResolveTag(tag, $"refs/tags/{tag}^{{}}");

    public string ResolveRemotePeeledTag(string tag)
    {
        var result = RunGit("ls-remote", "--tags", "origin", $"refs/tags/{tag}^{{}}");
        if (string.IsNullOrWhiteSpace(result)) result = RunGit("ls-remote", "--tags", "origin", $"refs/tags/{tag}");
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException($"Unable to resolve remote tag {tag}.");
        return result.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[0].Trim();
    }

    public bool IsCommitInMain(string commit) => RunGitExitCode("merge-base", "--is-ancestor", commit, "origin/main") == 0;

    private string ResolveTag(string tag, string reference)
    {
        var result = RunGit("rev-list", "-n", "1", reference);
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException($"Unable to resolve local tag {tag}.");
        return result;
    }

    private string RunGit(params string[] arguments)
    {
        using var process = CreateProcess(arguments);
        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {stderr}");
        return stdout.Trim();
    }

    private int RunGitExitCode(params string[] arguments)
    {
        using var process = CreateProcess(arguments); process.Start(); process.WaitForExit(); return process.ExitCode;
    }

    private Process CreateProcess(IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo { FileName = "git", WorkingDirectory = repositoryRoot, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        return new Process { StartInfo = startInfo };
    }
}