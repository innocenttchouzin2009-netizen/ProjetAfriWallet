using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class SecretBoundaryCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-008";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var disputeRoot = RepositoryCheckUtilities.Resolve(repositoryRoot, "backend", "src", "Disputes");
        var forbidden = new[]
        {
            "BEGIN PRIVATE " + "KEY",
            "BEGIN RSA PRIVATE " + "KEY",
            "BEGIN OPENSSH PRIVATE " + "KEY",
            "github_" + "pat_",
            "gh" + "p_"
        };

        var findings = new List<string>();
        foreach (var file in RepositoryCheckUtilities.EnumerateTextFiles(disputeRoot))
        {
            var content = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                    findings.Add($"{Path.GetFileName(file)}:{token}");
            }
        }

        return Task.FromResult(
            new ReadinessCheck(
                Code,
                "Dispute secret boundary",
                findings.Count == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                findings.Count == 0 ? "No embedded secret pattern detected" : string.Join(", ", findings)));
    }
}
