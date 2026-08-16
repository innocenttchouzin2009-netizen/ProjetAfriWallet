using AfriWallet.Compliance.Readiness.Models;
namespace AfriWallet.Compliance.Readiness.Checks;
public interface IComplianceReadinessCheck { string Code { get; } Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default); }