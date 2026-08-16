using System.Diagnostics;
namespace AfriWallet.Compliance.ReleaseCandidate.Services;
public sealed class GitDeliveryVerifier(string repositoryRoot)
{
    private readonly string _root=repositoryRoot;
    public bool LocalTagExists(string tag)=>RunExit("rev-parse","--verify",tag)==0;
    public bool RemoteTagExists(string tag)=>!string.IsNullOrWhiteSpace(Run("ls-remote","--tags","origin",$"refs/tags/{tag}"));
    public string ResolveTagCommit(string tag)=>Run("rev-list","-n","1",tag);
    public string ResolveRemotePeeledTag(string tag){var peeled=Run("ls-remote","--tags","origin",$"refs/tags/{tag}^{{}}");var value=string.IsNullOrWhiteSpace(peeled)?Run("ls-remote","--tags","origin",$"refs/tags/{tag}"):peeled;return value.Split([' ','\t'],StringSplitOptions.RemoveEmptyEntries)[0].Trim();}
    public bool IsCommitInMain(string sha)=>RunExit("merge-base","--is-ancestor",sha,"origin/main")==0;
    private string Run(params string[] args){using var p=Create(args);p.Start();var output=p.StandardOutput.ReadToEnd();var error=p.StandardError.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0)throw new InvalidOperationException($"git {string.Join(' ',args)} failed: {error}");return output.Trim();}
    private int RunExit(params string[] args){using var p=Create(args);p.Start();p.StandardOutput.ReadToEnd();p.StandardError.ReadToEnd();p.WaitForExit();return p.ExitCode;}
    private Process Create(IEnumerable<string> args){var info=new ProcessStartInfo{FileName="git",WorkingDirectory=_root,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};foreach(var arg in args)info.ArgumentList.Add(arg);return new Process{StartInfo=info};}
}