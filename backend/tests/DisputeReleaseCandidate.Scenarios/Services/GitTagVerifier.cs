using System.Diagnostics;

namespace AfriWallet.Disputes.ReleaseCandidate.Services;

public sealed class GitTagVerifier(string repositoryRoot)
{
    private readonly string _repositoryRoot = repositoryRoot;

    public string RepositoryRoot => _repositoryRoot;

    public string ResolvePeeledLocalSha(string tag) =>
        RunGit("rev-list", "-n", "1", $"{tag}^{{}}");

    public string ResolvePeeledRemoteSha(string tag)
    {
        var result = RunGit("ls-remote", "--tags", "origin", $"refs/tags/{tag}^{{}}");
        if (string.IsNullOrWhiteSpace(result))
        {
            result = RunGit("ls-remote", "--tags", "origin", $"refs/tags/{tag}");
        }
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException($"Unable to resolve remote tag {tag}.");

        return result
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim();
    }

    public bool IsInOriginMain(string sha) =>
        RunGitExitCode("merge-base", "--is-ancestor", sha, "origin/main") == 0;

    private string RunGit(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start git.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr}");

        return stdout.Trim();
    }

    private int RunGitExitCode(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _repositoryRoot,
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
