using AfriWallet.Disputes.Readiness.Checks;
using AfriWallet.Disputes.Readiness.Models;

namespace AfriWallet.Disputes.Readiness.Services;

public sealed class DisputeReadinessRunner
{
    private readonly IReadOnlyCollection<IDisputeReadinessCheck> _checks;

    public DisputeReadinessRunner(IEnumerable<IDisputeReadinessCheck> checks)
    {
        _checks = checks.ToArray();
    }

    public async Task<ReadinessReport> RunAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        var results = new List<ReadinessCheck>();
        foreach (var check in _checks)
        {
            try
            {
                results.Add(await check.ExecuteAsync(repositoryRoot, cancellationToken));
            }
            catch (Exception ex)
            {
                results.Add(new ReadinessCheck(check.Code, check.GetType().Name, ReadinessStatus.Failed, ex.Message));
            }
        }

        return new ReadinessReport(results);
    }
}
