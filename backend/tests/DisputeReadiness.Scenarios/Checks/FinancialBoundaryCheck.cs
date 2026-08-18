using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class FinancialBoundaryCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-004";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var disputeRoot = RepositoryCheckUtilities.Resolve(repositoryRoot, "backend", "src", "Disputes");
        var forbidden = new[]
        {
            "ExecuteRealRefundAsync(",
            "SubmitRealChargebackAsync(",
            "MoveMoneyAsync(",
            "DebitWalletAsync(",
            "CreditWalletAsync(",
            "ExecuteSettlementAsync("
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
                "Financial execution boundary",
                findings.Count == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                findings.Count == 0 ? "No direct real financial execution API detected" : string.Join(", ", findings)));
    }
}
