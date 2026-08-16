using System.Collections.Concurrent;
using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Alerts;

namespace AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

public sealed class InMemoryMonitoringAlertRepository : IMonitoringAlertRepository
{
    private readonly ConcurrentDictionary<Guid, MonitoringAlert> _alerts = new();

    public Task AddAsync(
        MonitoringAlert alert,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_alerts.TryAdd(alert.AlertId, alert))
            throw new InvalidOperationException("Monitoring alert already exists.");

        return Task.CompletedTask;
    }

    public Task<MonitoringAlert?> GetAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _alerts.TryGetValue(alertId, out var alert);
        return Task.FromResult(alert);
    }

    public Task<IReadOnlyCollection<MonitoringAlert>> GetByAwidAsync(
        string awid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<MonitoringAlert> result = _alerts.Values
            .Where(alert => string.Equals(alert.Awid, awid, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}