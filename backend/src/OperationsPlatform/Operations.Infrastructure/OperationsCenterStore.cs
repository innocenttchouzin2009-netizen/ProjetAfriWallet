using Operations.Domain;

namespace Operations.Infrastructure;

public sealed class OperationsCenterStore
{
    public List<ServiceHealth> Services { get; } = new();

    public List<Incident> Incidents { get; } = new();

    public List<Alert> Alerts { get; } = new();

    public List<MaintenanceWindow> MaintenanceWindows { get; } = new();

    public List<DeploymentRecord> Deployments { get; } = new();

    public List<BackupSnapshot> Backups { get; } = new();

    public List<DisasterRecoveryPlan> DisasterRecoveryPlans { get; } = new();

    public Dictionary<string, long> Metrics { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["afw_service_health"] = 0,
        ["afw_incidents_open"] = 0,
        ["afw_alerts_active"] = 0,
        ["afw_backup_success"] = 0,
        ["afw_backup_failure"] = 0,
        ["afw_deployment_total"] = 0,
        ["afw_uptime_percent"] = 0
    };

    public List<string> AuditTrail { get; } = new();

    public OperationsCenterStore()
    {
        SeedServices();
        SeedIncidents();
        SeedAlerts();
        SeedMaintenance();
        SeedDeployments();
        SeedBackups();
        SeedDisasterRecovery();
    }

    private void SeedServices()
    {
        AddService("identity", HealthStatus.Healthy, TimeSpan.FromMilliseconds(180), 99.99);
        AddService("wallet", HealthStatus.Degraded, TimeSpan.FromMilliseconds(620), 97.40);
        AddService("merchant", HealthStatus.Healthy, TimeSpan.FromMilliseconds(210), 99.95);
        AddService("risk", HealthStatus.Unhealthy, TimeSpan.FromMilliseconds(0), 94.20);
        AddService("notifications", HealthStatus.Maintenance, TimeSpan.FromMilliseconds(0), 100);
        AddService("reporting", HealthStatus.Healthy, TimeSpan.FromMilliseconds(340), 99.91);
    }

    private void SeedIncidents()
    {
        var paymentIncident = new Incident
        {
            Severity = IncidentSeverity.High,
            ServiceName = "wallet",
            Summary = "Wallet transfer latency spike",
            Description = "Wallet transfers are taking longer than the 95th percentile SLO during the current release window."
        };
        paymentIncident.StartProgress("sre-oncall", "Investigation started");
        Incidents.Add(paymentIncident);

        var riskIncident = new Incident
        {
            Severity = IncidentSeverity.Critical,
            ServiceName = "risk",
            Summary = "Risk scoring service unavailable",
            Description = "The risk scoring API is not responding to live traffic and requires immediate intervention."
        };
        Incidents.Add(riskIncident);
    }

    private void SeedAlerts()
    {
        var alert = new Alert
        {
            Severity = AlertSeverity.Critical,
            Metric = "afw_uptime_percent",
            Threshold = 99.90m
        };
        alert.UpdateCurrentValue(97.82m);
        Alerts.Add(alert);
    }

    private void SeedMaintenance()
    {
        MaintenanceWindows.Add(new MaintenanceWindow
        {
            Reason = "Quarterly security patching and capacity tuning",
            StartUtc = DateTimeOffset.UtcNow.AddHours(12),
            EndUtc = DateTimeOffset.UtcNow.AddHours(14),
            ApprovedBy = "ops-director",
            Services = new List<string> { "notifications", "reporting" }
        });
    }

    private void SeedDeployments()
    {
        Deployments.Add(new DeploymentRecord
        {
            ServiceName = "wallet",
            Version = "2026.08.07-rc1",
            Environment = "production",
            DeployedBy = "release-engine"
        });

        Deployments.Add(new DeploymentRecord
        {
            ServiceName = "merchant",
            Version = "2026.08.07-rc1",
            Environment = "production",
            DeployedBy = "release-engine"
        });
    }

    private void SeedBackups()
    {
        Backups.Add(new BackupSnapshot
        {
            StorageProvider = "azure-blob",
            Region = "westeurope",
            Encrypted = true,
            Checksum = "afw-backup-20260807-001",
            Status = BackupStatus.Succeeded
        });

        Backups.Add(new BackupSnapshot
        {
            StorageProvider = "aws-s3",
            Region = "af-south-1",
            Encrypted = true,
            Checksum = "afw-backup-20260807-002",
            Status = BackupStatus.Succeeded
        });
    }

    private void SeedDisasterRecovery()
    {
        DisasterRecoveryPlans.Add(new DisasterRecoveryPlan
        {
            Region = "westeurope",
            RecoveryTimeObjective = TimeSpan.FromHours(2),
            RecoveryPointObjective = TimeSpan.FromMinutes(15),
            Owner = "platform-reliability"
        });
    }

    private void AddService(string serviceName, HealthStatus status, TimeSpan responseTime, double availabilityPercent)
    {
        var service = new ServiceHealth
        {
            ServiceName = serviceName
        };

        switch (status)
        {
            case HealthStatus.Healthy:
                service.ReportHealthy(responseTime);
                break;
            case HealthStatus.Degraded:
                service.ReportDegraded(responseTime);
                break;
            case HealthStatus.Maintenance:
                service.ReportMaintenance();
                break;
            case HealthStatus.Unhealthy:
                service.ReportFailure();
                break;
        }

        service.SetAvailabilityPercent(availabilityPercent);
        Services.Add(service);
    }
}