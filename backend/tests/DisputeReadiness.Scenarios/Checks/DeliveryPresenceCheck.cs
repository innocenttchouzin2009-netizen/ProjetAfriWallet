using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class DeliveryPresenceCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-001";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var required = new[]
        {
            "backend/src/Disputes/DisputeRegistry.Domain",
            "backend/src/Disputes/DisputeEligibility.Domain",
            "backend/src/Disputes/DisputeInvestigation.Domain",
            "backend/src/Disputes/DisputeDecision",
            "backend/src/Disputes/ResolutionOrchestration",
            "backend/src/Disputes/DisputeIntelligence"
        };

        var missing = required
            .Where(path => !Directory.Exists(RepositoryCheckUtilities.Resolve(repositoryRoot, path.Split('/'))))
            .ToArray();

        return Task.FromResult(
            new ReadinessCheck(
                Code,
                "Dispute deliveries present",
                missing.Length == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                missing.Length == 0 ? "0018.1 through 0018.6 present" : $"Missing: {string.Join(", ", missing)}"));
    }
}
