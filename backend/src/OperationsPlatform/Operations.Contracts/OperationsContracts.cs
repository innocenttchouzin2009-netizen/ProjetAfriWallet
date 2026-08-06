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
