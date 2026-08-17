using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class ArchitectureBoundaryCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-003";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var required = new[] { "FraudSignals.Domain", "DeviceRisk.Domain", "TransactionFraud.Domain", "FraudDecision.Domain", "FraudInvestigation.Domain", "FraudIntelligence.Domain" };
        var fraudRoot = RepositoryCheckUtilities.Resolve(root, "backend", "src", "Fraud");
        var missing = required.Where(x => !Directory.Exists(Path.Combine(fraudRoot, x))).ToArray();
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Fraud architecture boundaries", missing.Length == 0, missing.Length == 0 ? "Six fraud bounded contexts present" : $"Missing: {string.Join(", ", missing)}"));
    }
}