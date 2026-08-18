using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class LedgerBoundaryCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-005";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var disputeRoot = RepositoryCheckUtilities.Resolve(repositoryRoot, "backend", "src", "Disputes");
        var forbidden = new[]
        {
            "PostLedgerEntryAsync(",
            "AppendLedgerEntryAsync(",
            "ReverseLedgerEntryAsync(",
            "WriteLedgerAsync(",
            "ILedgerWriter",
            "IUniversalLedgerWriter"
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
                "Universal Ledger boundary",
                findings.Count == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                findings.Count == 0 ? "No direct ledger writer dependency detected" : string.Join(", ", findings)));
    }
}
