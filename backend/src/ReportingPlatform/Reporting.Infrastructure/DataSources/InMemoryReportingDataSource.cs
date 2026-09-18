using Reporting.Application.Interfaces;

namespace Reporting.Infrastructure.DataSources;

public sealed class InMemoryReportingDataSource : IReportingDataSource
{
    public Task<decimal> GetPaymentVolumeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(12_500_000m);
    }

    public Task<long> GetTransactionCountAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(4_820L);
    }

    public Task<long> GetActiveMerchantCountAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult(326L);
    }

    public Task<long> GetOpenSupportCaseCountAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult(42L);
    }
}
