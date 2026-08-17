using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class DeliveryPresenceCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-001";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var required = new[] { "FraudSignals.Domain", "DeviceRisk.Domain", "TransactionFraud.Domain", "FraudDecision.Domain", "FraudInvestigation.Domain", "FraudIntelligence.Domain" };
        var missing = required.Where(x => !Directory.Exists(RepositoryCheckUtilities.Resolve(root, "backend", "src", "Fraud", x))).ToArray();
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Required fraud deliveries present", missing.Length == 0, missing.Length == 0 ? "0017.1 through 0017.6 present" : $"Missing: {string.Join(", ", missing)}"));
    }
}