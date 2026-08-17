using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public interface IFraudReadinessCheck
{
    string Code { get; }
    Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default);
}