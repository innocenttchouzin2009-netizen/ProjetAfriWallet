using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public sealed class DeterministicIntelligenceCheck : IDisputeReadinessCheck
{
    public string Code => "DSP-RDY-007";

    public Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var disputeRoot = RepositoryCheckUtilities.Resolve(repositoryRoot, "backend", "src", "Disputes");
        var forbidden = new[]
        {
            "Microsoft.ML",
            "TensorFlow",
            "ONNXRuntime",
            "PredictionEngine",
            "MLContext"
        };

        var findings = new List<string>();
        foreach (var file in RepositoryCheckUtilities.EnumerateTextFiles(disputeRoot))
        {
            var content = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                if (content.Contains(token, StringComparison.OrdinalIgnoreCase))
                    findings.Add($"{Path.GetFileName(file)}:{token}");
            }
        }

        return Task.FromResult(
            new ReadinessCheck(
                Code,
                "Deterministic dispute intelligence",
                findings.Count == 0 ? ReadinessStatus.Passed : ReadinessStatus.Failed,
                findings.Count == 0 ? "No opaque ML dependency detected" : string.Join(", ", findings)));
    }
}
