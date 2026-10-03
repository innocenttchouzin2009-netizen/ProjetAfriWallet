namespace Reporting.Application.Interfaces;

public interface IReportingDataSource
{
    Task<decimal> GetPaymentVolumeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<long> GetTransactionCountAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<long> GetActiveMerchantCountAsync(
        CancellationToken cancellationToken);

    Task<long> GetOpenSupportCaseCountAsync(
        CancellationToken cancellationToken);
}
