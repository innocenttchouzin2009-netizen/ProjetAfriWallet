using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class AuditCapabilityCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-007";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var required = new[] { "DeviceRisk.Application/Abstractions/IDeviceRiskAuditStore.cs", "TransactionFraud.Application/Abstractions/ITransactionFraudAuditStore.cs", "FraudDecision.Application/Abstractions/IFraudDecisionAuditStore.cs", "FraudInvestigation.Application/Abstractions/IFraudInvestigationAuditStore.cs", "FraudIntelligence.Application/Abstractions/IFraudIntelligenceAuditStore.cs" };
        var missing = required.Where(x => !File.Exists(RepositoryCheckUtilities.Resolve(root, new[] { "backend", "src", "Fraud" }.Concat(x.Split('/')).ToArray()))).ToArray();
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Fraud audit capability", missing.Length == 0, missing.Length == 0 ? "Audit abstractions present across fraud platform" : $"Missing: {string.Join(", ", missing)}"));
    }
}