namespace Operations.Contracts;

public sealed class OperationsContextRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool HasMfa { get; set; }
    public bool HasDeviceTrust { get; set; }
}

public sealed class OperationsSearchRequest
{
    public string? Query { get; set; }
    public string? Awid { get; set; }
    public string? TransactionId { get; set; }
    public string? WalletId { get; set; }
    public string? CardId { get; set; }
    public string? BeneficiaryId { get; set; }
    public string? MerchantId { get; set; }
}

public sealed class OperationsDashboardResponse
{
    public long OpenSupportCases { get; set; }
    public long OpenRiskAlerts { get; set; }
    public long OpenComplianceCases { get; set; }
    public long ActiveWallets { get; set; }
    public long ProcessingPayments { get; set; }
    public long ServiceIncidents { get; set; }
    public long ServicesHealthy { get; set; }
    public long ServicesDegraded { get; set; }
    public long ServicesDown { get; set; }
    public long ActiveIncidents { get; set; }
    public long ActiveAlerts { get; set; }
    public long DeploymentsToday { get; set; }
    public long Backups { get; set; }
    public long Maintenance { get; set; }
    public double UptimePercent { get; set; }
    public Dictionary<string, string> HealthSummary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, long> Metrics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class OperationsSearchResponse
{
    public List<OperationsSearchItem> Items { get; set; } = new();
}

public sealed class OperationsSearchItem
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string MaskedData { get; set; } = string.Empty;
}

public sealed class OperationsUserResponse
{
    public string Awid { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EmailMasked { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLabel { get; set; } = string.Empty;
    public string SupportSummary { get; set; } = string.Empty;
}

public sealed class OperationsTransactionResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string Awid { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string? CardId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<string> Timeline { get; set; } = new();
    public string MaskedData { get; set; } = string.Empty;
}

public sealed class OperationsHealthResponse
{
    public Dictionary<string, string> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public double UptimePercent { get; set; }
}

public sealed class OperationsAuditResponse
{
    public List<string> Events { get; set; } = new();
}

public sealed class SuspendWalletRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string ConfirmedBy { get; set; } = string.Empty;
}

public sealed class FreezeCardRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string ConfirmedBy { get; set; } = string.Empty;
}

public sealed class AssignCaseRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
}

public sealed class RetryTransactionRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string ConfirmedBy { get; set; } = string.Empty;
}

public sealed class CreateIncidentRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
}

public sealed class AcknowledgeIncidentRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public sealed class ResolveIncidentRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
}

public sealed class OperationsIncidentResponse
{
    public Guid IncidentId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OpenedUtc { get; set; }
    public DateTimeOffset? AcknowledgedUtc { get; set; }
    public DateTimeOffset? ResolvedUtc { get; set; }
    public DateTimeOffset? ClosedUtc { get; set; }
    public string Owner { get; set; } = string.Empty;
    public List<string> Timeline { get; set; } = new();
}

public sealed class OperationsIncidentListResponse
{
    public List<OperationsIncidentResponse> Items { get; set; } = new();
}

public sealed class OperationsAlertResponse
{
    public Guid AlertId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public decimal Threshold { get; set; }
    public decimal CurrentValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public bool Acknowledged { get; set; }
    public bool Escalated { get; set; }
    public DateTimeOffset RaisedUtc { get; set; }
}

public sealed class OperationsAlertListResponse
{
    public List<OperationsAlertResponse> Items { get; set; } = new();
}

public sealed class MaintenanceWindowRequest
{
    public string ActorId { get; set; } = string.Empty;
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> Services { get; set; } = new();
    public string ApprovedBy { get; set; } = string.Empty;
}

public sealed class MaintenanceWindowResponse
{
    public Guid WindowId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> Services { get; set; } = new();
    public string ApprovedBy { get; set; } = string.Empty;
}

public sealed class MaintenanceWindowListResponse
{
    public List<MaintenanceWindowResponse> Items { get; set; } = new();
}

public sealed class OperationsDeploymentResponse
{
    public Guid DeploymentId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset DeployedUtc { get; set; }
    public string DeployedBy { get; set; } = string.Empty;
    public List<string> Timeline { get; set; } = new();
}

public sealed class OperationsDeploymentListResponse
{
    public List<OperationsDeploymentResponse> Items { get; set; } = new();
}

public sealed class OperationsBackupResponse
{
    public Guid SnapshotId { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public bool Encrypted { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class OperationsBackupListResponse
{
    public List<OperationsBackupResponse> Items { get; set; } = new();
}

public sealed class OperationsDisasterRecoveryResponse
{
    public Guid PlanId { get; set; }
    public string Region { get; set; } = string.Empty;
    public TimeSpan RecoveryTimeObjective { get; set; }
    public TimeSpan RecoveryPointObjective { get; set; }
    public DateTimeOffset? LastTestUtc { get; set; }
}

public sealed class OperationsDisasterRecoveryListResponse
{
    public List<OperationsDisasterRecoveryResponse> Items { get; set; } = new();
}
