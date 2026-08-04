namespace MobileMoney.Production.Audit;

public sealed class AuditRecord
{
    public string AuditId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
    public string? TransactionId { get; init; }
    public string? ProviderReference { get; init; }
    public string? AwidId { get; init; }
    public string? WalletId { get; init; }
    public string? ProviderCode { get; init; }
    public string? OperationType { get; init; }
    public AuditAction Action { get; init; }
    public AuditCategory Category { get; init; }
    public AuditResult Result { get; init; }
    public string? ActorType { get; init; }
    public string? ActorId { get; init; }
    public string? Environment { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceId { get; init; }
    public string? PhoneNumber { get; set; }
    public long DurationMs { get; init; }
    public string? PreviousAuditHash { get; set; }
    public string? CurrentAuditHash { get; set; }
}
