using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class SecretBoundaryCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-005";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var patterns = new[]
        {
            "BEGIN PRIVATE " + "KEY",
            "BEGIN RSA PRIVATE " + "KEY",
            "BEGIN OPENSSH PRIVATE " + "KEY",
            "github" + "_pat_",
            "gh" + "p_"
        };
        var findings = RepositoryCheckUtilities.EnumerateTextFiles(RepositoryCheckUtilities.Resolve(root, "backend", "src", "Fraud")).SelectMany(file => patterns.Where(pattern => File.ReadAllText(file).Contains(pattern, StringComparison.Ordinal)).Select(pattern => $"{Path.GetFileName(file)}:{pattern}")).ToArray();
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Embedded secret boundary", findings.Length == 0, findings.Length == 0 ? "No embedded secret pattern detected" : string.Join(", ", findings)));
    }
}