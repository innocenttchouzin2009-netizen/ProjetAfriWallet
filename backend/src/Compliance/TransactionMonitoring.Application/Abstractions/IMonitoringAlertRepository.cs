using AfriWallet.Compliance.TransactionMonitoring.Domain.Alerts;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

public interface IMonitoringAlertRepository
{
    Task AddAsync(
        MonitoringAlert alert,
        CancellationToken cancellationToken = default);

    Task<MonitoringAlert?> GetAsync(
        Guid alertId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MonitoringAlert>> GetByAwidAsync(
        string awid,
        CancellationToken cancellationToken = default);
}