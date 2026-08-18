using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class AuditCapabilityCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-006";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var disputeRoot = RepositoryCheckUtilities.Resolve(repositoryRoot, "backend", "src", "Disputes");
        var requiredTokens = new[]
        {
            "IDisputeEligibilityAuditStore",
            "IDisputeInvestigationAuditStore",
            "IDisputeDecisionAuditStore",
            "IResolutionAuditStore",
            "IDisputeIntelligenceAuditStore"
        };

        var files = RepositoryCheckUtilities.EnumerateTextFiles(disputeRoot).ToArray();

        var missing = requiredTokens
            .Where(token => !files.Any(file => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)))
            .ToArray();

        return Task.FromResult(
            new ReadinessCheck(
                Code,
                "Dispute audit capability",
                missing.Length == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                missing.Length == 0 ? "Audit abstractions present across dispute platform" : $"Missing: {string.Join(", ", missing)}"));
    }
}
