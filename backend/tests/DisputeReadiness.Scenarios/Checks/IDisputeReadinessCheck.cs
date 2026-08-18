using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Checks;

public interface IDisputeReadinessCheck
{
    string Code { get; }
    Task<ReadinessCheck> ExecuteAsync(string repositoryRoot, CancellationToken cancellationToken = default);
}
