using AfriWallet.Fraud.Readiness.Checks;
using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Services;

public sealed class FraudReadinessRunner(IEnumerable<IFraudReadinessCheck> checks)
{
    private readonly IReadOnlyCollection<IFraudReadinessCheck> checks = checks.ToArray();

    public async Task<ReadinessReport> RunAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        var results = new List<ReadinessCheck>();
        foreach (var check in checks)
        {
            try { results.Add(await check.ExecuteAsync(repositoryRoot, cancellationToken)); }
            catch (Exception ex) { results.Add(new ReadinessCheck(check.Code, check.GetType().Name, ReadinessStatus.Failed, ex.Message)); }
        }
        return new ReadinessReport(results);
    }
}