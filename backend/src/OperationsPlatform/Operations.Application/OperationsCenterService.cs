using Operations.Contracts;
using Operations.Domain;
using Operations.Infrastructure;

namespace Operations.Application;

public sealed class OperationsCenterService
{
    private readonly OperationsCenterStore _store;
    private readonly OperationsCenterAuthorizationService _authorizationService;
    private readonly OperationsHealthAggregatorService _healthAggregatorService;

    public OperationsCenterService(
        OperationsCenterStore store,
        OperationsCenterAuthorizationService authorizationService,
        OperationsHealthAggregatorService healthAggregatorService)
    {
        _store = store;
        _authorizationService = authorizationService;
        _healthAggregatorService = healthAggregatorService;
    }

    public OperationsHealthResponse GetHealth(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        _store.AuditTrail.Add($"{DateTimeOffset.UtcNow:O} HEALTH_VIEW {context.ActorId}");
        return _healthAggregatorService.BuildHealthResponse();
    }

    public OperationsDashboardResponse GetDashboard(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        _store.AuditTrail.Add($"{DateTimeOffset.UtcNow:O} DASHBOARD_VIEW {context.ActorId}");
        return _healthAggregatorService.BuildDashboardResponse();
    }

    public OperationsIncidentListResponse GetIncidents(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        return new OperationsIncidentListResponse
        {
            Items = _store.Incidents.Select(ToResponse).ToList()
        };
    }

    public OperationsIncidentResponse CreateIncident(CreateIncidentRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureControlAccess(context, requireMfa: true);
        var incident = new Incident
        {
            ServiceName = request.ServiceName,
            Severity = ParseIncidentSeverity(request.Severity),
            Summary = request.Summary,
            Description = request.Description
        };

        if (!string.IsNullOrWhiteSpace(request.Owner))
        {
            incident.Acknowledge(request.Owner, "Incident created and assigned.");
        }

        _store.Incidents.Add(incident);
        _store.Metrics["afw_incidents_open"] += 1;
        _store.AuditTrail.Add($"{DateTimeOffset.UtcNow:O} INCIDENT_CREATED {incident.IncidentId} {context.ActorId}");
        return ToResponse(incident);
    }

    public OperationsIncidentResponse AcknowledgeIncident(Guid incidentId, AcknowledgeIncidentRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureControlAccess(context, requireMfa: true);
        var incident = _store.Incidents.Single(item => item.IncidentId == incidentId);
        incident.Acknowledge(request.Owner, request.Note);
        _store.AuditTrail.Add($"{DateTimeOffset.UtcNow:O} INCIDENT_ACK {incident.IncidentId} {context.ActorId}");
        return ToResponse(incident);
    }

    public OperationsIncidentResponse ResolveIncident(Guid incidentId, ResolveIncidentRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureControlAccess(context, requireMfa: true, requireDeviceTrust: true);
        var incident = _store.Incidents.Single(item => item.IncidentId == incidentId);
        incident.StartProgress(request.ActorId, "Work in progress before resolution");
        incident.Resolve(request.Resolution);
        incident.Close("Incident closure confirmed by operations");
        _store.Metrics["afw_incidents_open"] = Math.Max(0, _store.Metrics["afw_incidents_open"] - 1);
        _store.AuditTrail.Add($"{DateTimeOffset.UtcNow:O} INCIDENT_RESOLVED {incident.IncidentId} {context.ActorId}");
        return ToResponse(incident);
    }

    public OperationsAlertListResponse GetAlerts(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        return new OperationsAlertListResponse
        {
            Items = _store.Alerts.Select(ToResponse).ToList()
        };
    }

    public MaintenanceWindowResponse ScheduleMaintenance(MaintenanceWindowRequest request, OperationsContextRequest context)
    {
        _authorizationService.EnsureControlAccess(context, requireMfa: true, requireDeviceTrust: true);
        var window = new MaintenanceWindow
        {
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            Reason = request.Reason,
            ApprovedBy = request.ApprovedBy,
            Services = request.Services.ToList()
        };

        _store.MaintenanceWindows.Add(window);
        _store.AuditTrail.Add($"{DateTimeOffset.UtcNow:O} MAINTENANCE_SCHEDULED {window.WindowId} {context.ActorId}");
        return ToResponse(window);
    }

    public OperationsDeploymentListResponse GetDeployments(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        return new OperationsDeploymentListResponse
        {
            Items = _store.Deployments.Select(ToResponse).ToList()
        };
    }

    public OperationsBackupListResponse GetBackups(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        return new OperationsBackupListResponse
        {
            Items = _store.Backups.Select(ToResponse).ToList()
        };
    }

    public OperationsDisasterRecoveryListResponse GetDisasterRecovery(OperationsContextRequest context)
    {
        _authorizationService.EnsureReadAccess(context);
        return new OperationsDisasterRecoveryListResponse
        {
            Items = _store.DisasterRecoveryPlans.Select(ToResponse).ToList()
        };
    }

    private static IncidentSeverity ParseIncidentSeverity(string severity)
    {
        if (Enum.TryParse<IncidentSeverity>(severity, true, out var parsed))
        {
            return parsed;
        }

        return IncidentSeverity.Medium;
    }

    private static OperationsIncidentResponse ToResponse(Incident incident) => new()
    {
        IncidentId = incident.IncidentId,
        Severity = incident.Severity.ToString().ToUpperInvariant(),
        Status = incident.Status.ToString().ToUpperInvariant(),
        ServiceName = incident.ServiceName,
        Summary = incident.Summary,
        Description = incident.Description,
        OpenedUtc = incident.OpenedUtc,
        AcknowledgedUtc = incident.AcknowledgedUtc,
        ResolvedUtc = incident.ResolvedUtc,
        ClosedUtc = incident.ClosedUtc,
        Owner = incident.Owner,
        Timeline = incident.Timeline.ToList()
    };

    private static OperationsAlertResponse ToResponse(Alert alert) => new()
    {
        AlertId = alert.AlertId,
        Metric = alert.Metric,
        Threshold = alert.Threshold,
        CurrentValue = alert.CurrentValue,
        Severity = alert.Severity.ToString().ToUpperInvariant(),
        Acknowledged = alert.Acknowledged,
        Escalated = alert.Escalated,
        RaisedUtc = alert.RaisedUtc
    };

    private static MaintenanceWindowResponse ToResponse(MaintenanceWindow window) => new()
    {
        WindowId = window.WindowId,
        StartUtc = window.StartUtc,
        EndUtc = window.EndUtc,
        Reason = window.Reason,
        Services = window.Services.ToList(),
        ApprovedBy = window.ApprovedBy
    };

    private static OperationsDeploymentResponse ToResponse(DeploymentRecord deployment) => new()
    {
        DeploymentId = deployment.DeploymentId,
        ServiceName = deployment.ServiceName,
        Version = deployment.Version,
        Environment = deployment.Environment,
        Status = deployment.Status.ToString().ToUpperInvariant(),
        DeployedUtc = deployment.DeployedUtc,
        DeployedBy = deployment.DeployedBy,
        Timeline = deployment.Timeline.ToList()
    };

    private static OperationsBackupResponse ToResponse(BackupSnapshot snapshot) => new()
    {
        SnapshotId = snapshot.SnapshotId,
        StorageProvider = snapshot.StorageProvider,
        Region = snapshot.Region,
        CreatedUtc = snapshot.CreatedUtc,
        Encrypted = snapshot.Encrypted,
        Checksum = snapshot.Checksum,
        Status = snapshot.Status.ToString().ToUpperInvariant()
    };

    private static OperationsDisasterRecoveryResponse ToResponse(DisasterRecoveryPlan plan) => new()
    {
        PlanId = plan.PlanId,
        Region = plan.Region,
        RecoveryTimeObjective = plan.RecoveryTimeObjective,
        RecoveryPointObjective = plan.RecoveryPointObjective,
        LastTestUtc = plan.LastTestUtc
    };
}