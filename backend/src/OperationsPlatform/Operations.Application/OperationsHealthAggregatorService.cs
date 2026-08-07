using Operations.Contracts;
using Operations.Domain;
using Operations.Infrastructure;

namespace Operations.Application;

public sealed class OperationsHealthAggregatorService
{
    private readonly OperationsCenterStore _store;

    public OperationsHealthAggregatorService(OperationsCenterStore store)
    {
        _store = store;
    }

    public OperationsHealthResponse BuildHealthResponse()
    {
        var services = _store.Services.ToDictionary(
            service => service.ServiceName,
            service => service.Status.ToString().ToUpperInvariant(),
            StringComparer.OrdinalIgnoreCase);

        return new OperationsHealthResponse
        {
            Services = services,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            UptimePercent = CalculateUptimePercent()
        };
    }

    public OperationsDashboardResponse BuildDashboardResponse()
    {
        var servicesHealthy = _store.Services.Count(service => service.Status == HealthStatus.Healthy);
        var servicesDegraded = _store.Services.Count(service => service.Status == HealthStatus.Degraded);
        var servicesDown = _store.Services.Count(service => service.Status == HealthStatus.Unhealthy);
        var activeIncidents = _store.Incidents.Count(incident => incident.Status is not IncidentStatus.Resolved and not IncidentStatus.Closed);
        var activeAlerts = _store.Alerts.Count(alert => !alert.Acknowledged);
        var deploymentsToday = _store.Deployments.Count(deployment => deployment.DeployedUtc.UtcDateTime.Date == DateTime.UtcNow.Date);
        var backups = _store.Backups.Count(snapshot => snapshot.Status == BackupStatus.Succeeded);
        var maintenance = _store.MaintenanceWindows.Count(window => window.IsActive(DateTimeOffset.UtcNow) || window.StartUtc > DateTimeOffset.UtcNow);

        var dashboard = new OperationsDashboardResponse
        {
            ServicesHealthy = servicesHealthy,
            ServicesDegraded = servicesDegraded,
            ServicesDown = servicesDown,
            ActiveIncidents = activeIncidents,
            ActiveAlerts = activeAlerts,
            DeploymentsToday = deploymentsToday,
            Backups = backups,
            Maintenance = maintenance,
            UptimePercent = CalculateUptimePercent(),
            HealthSummary = _store.Services.ToDictionary(
                service => service.ServiceName,
                service => service.Status.ToString(),
                StringComparer.OrdinalIgnoreCase),
            Metrics = BuildMetricsSnapshot()
        };

        dashboard.OpenSupportCases = 3;
        dashboard.OpenRiskAlerts = activeAlerts;
        dashboard.OpenComplianceCases = 1;
        dashboard.ActiveWallets = 12;
        dashboard.ProcessingPayments = 4;
        dashboard.ServiceIncidents = activeIncidents;
        return dashboard;
    }

    public Dictionary<string, long> BuildMetricsSnapshot()
    {
        var metrics = new Dictionary<string, long>(_store.Metrics, StringComparer.OrdinalIgnoreCase)
        {
            ["afw_service_health"] = _store.Services.Count,
            ["afw_incidents_open"] = _store.Incidents.Count(incident => incident.Status is not IncidentStatus.Resolved and not IncidentStatus.Closed),
            ["afw_alerts_active"] = _store.Alerts.Count(alert => !alert.Acknowledged),
            ["afw_backup_success"] = _store.Backups.Count(snapshot => snapshot.Status == BackupStatus.Succeeded),
            ["afw_backup_failure"] = _store.Backups.Count(snapshot => snapshot.Status == BackupStatus.Failed),
            ["afw_deployment_total"] = _store.Deployments.Count,
            ["afw_uptime_percent"] = (long)Math.Round(CalculateUptimePercent())
        };

        return metrics;
    }

    private double CalculateUptimePercent()
    {
        if (_store.Services.Count == 0)
        {
            return 100;
        }

        var total = _store.Services.Sum(service => service.AvailabilityPercent);
        return Math.Round(total / _store.Services.Count, 2);
    }
}