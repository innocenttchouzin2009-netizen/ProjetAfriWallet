using Reporting.Application.Interfaces;
using Reporting.Domain.Dashboards;
using Reporting.Domain.Metrics;

namespace Reporting.Application.Services;

public sealed class ReportingDashboardService
{
    private readonly IReportingDataSource _dataSource;

    public ReportingDashboardService(IReportingDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<DashboardSnapshot> BuildExecutiveDashboardAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (fromUtc >= toUtc)
        {
            throw new ArgumentException("The reporting period is invalid.");
        }

        var paymentVolume = await _dataSource.GetPaymentVolumeAsync(fromUtc, toUtc, cancellationToken);
        var transactionCount = await _dataSource.GetTransactionCountAsync(fromUtc, toUtc, cancellationToken);
        var merchantCount = await _dataSource.GetActiveMerchantCountAsync(cancellationToken);
        var openCases = await _dataSource.GetOpenSupportCaseCountAsync(cancellationToken);

        var metrics = new List<BusinessMetric>
        {
            new(
                "PAYMENT_VOLUME",
                "Payment volume",
                paymentVolume,
                "minor_currency_unit",
                DateTime.UtcNow),
            new(
                "TRANSACTION_COUNT",
                "Transaction count",
                transactionCount,
                "count",
                DateTime.UtcNow),
            new(
                "ACTIVE_MERCHANTS",
                "Active merchants",
                merchantCount,
                "count",
                DateTime.UtcNow),
            new(
                "OPEN_SUPPORT_CASES",
                "Open support cases",
                openCases,
                "count",
                DateTime.UtcNow)
        };

        var alerts = new List<DashboardAlert>();

        if (openCases > 100)
        {
            alerts.Add(new DashboardAlert(
                "SUPPORT_BACKLOG_HIGH",
                "WARNING",
                "The number of open support cases is above the configured threshold.",
                DateTime.UtcNow));
        }

        return new DashboardSnapshot(DateTime.UtcNow, metrics, alerts);
    }
}
