using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class ArchitectureBoundaryCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-003";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = RepositoryCheckUtilities.Resolve(repositoryRoot, "backend", "src", "Disputes");
        var requiredNames = new[]
        {
            "DisputeEligibility",
            "DisputeInvestigation",
            "DisputeDecision",
            "ResolutionOrchestration",
            "DisputeIntelligence"
        };

        var missing = requiredNames
            .Where(name => !Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                .Any(path => path.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return Task.FromResult(
            new ReadinessCheck(
                Code,
                "Dispute architecture boundaries",
                missing.Length == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                missing.Length == 0 ? "Expected bounded contexts present" : $"Missing: {string.Join(", ", missing)}"));
    }
}
